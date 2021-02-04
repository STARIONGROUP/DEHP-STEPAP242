
namespace DEHPSTEPAP242.MappingRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.ExceptionServices;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Rows;

    using NLog;

    /// <summary>
    /// Helper class which keeps a reference to the <see cref="ValueArray{string}"/> 
    /// that needs to me updated with the new <see cref="FileRevision"/> of the source
    /// STEP 3D file in the Hub.
    /// </summary>
    public class Step3dTargetSourceParameter
    {
        /// <summary>
        /// The <see cref="ValueArray{string}"/> of the <see cref="IValueSet"/> of interest
        /// </summary>
        public ValueArray<string> values;

        /// <summary>
        /// The index in the <see cref="ValueArray{string}"/> for the <see cref="ParameterTypeComponent"/> corresponding to the "source" field
        /// </summary>
        private int componentIndex;

        public Step3dTargetSourceParameter(ValueArray<string> values, int componentIndex)
        {
            this.values = values;
            this.componentIndex = componentIndex;
        }

        public void UpdateSource(FileRevision fileRevision)
        {
            this.values[componentIndex] = fileRevision.Iid.ToString();
        }
    }

    /// <summary>
    /// The <see cref="Step3dPartToElementDefinitionRule"/> is a <see cref="IMappingRule"/> 
    /// for the <see cref="MappingEngine"/>.
    /// 
    /// That takes a <see cref="List{T}"/> of <see cref="Step3dRowViewModel"/> as input 
    /// and outputs a E-TM-10-25 <see cref="ElementDefinition"/>.
    /// </summary>
    public class Step3dPartToElementDefinitionRule : MappingRule<List<Step3dRowViewModel>, (IEnumerable<ElementDefinition>, IEnumerable<Step3dTargetSourceParameter>)>
    {
        /// <summary>
        /// The current class logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController = AppContainer.Container.Resolve<IHubController>();

        /// <summary>
        /// Gets the <see cref="idCorrespondences"/>
        /// </summary>
        private List<IdCorrespondence> idCorrespondences;

        /// <summary>
        /// The <see cref="List{Step3dTargetSourceParameter}>"/> that needs to be updated 
        /// before the transfer to the Hub.
        /// </summary>
        private List<Step3dTargetSourceParameter> targetSourceParameters;

        /// <summary>
        /// The current <see cref="DomainOfExpertise"/>
        /// </summary>
        private DomainOfExpertise owner;

        /// <summary>
        /// Holds the current processing <see cref="Step3dRowViewModel"/> element name
        /// </summary>
        private string dstElementName;

        /// <summary>
        /// Holds the current processing <see cref="Step3dRowViewModel"/> parameter name
        /// </summary>
        private string dstParameterName;

        /// <summary>
        /// Transforms a <see cref="List{T}"/> of <see cref="Step3dRowViewModel"/> into an <see cref="ElementDefinition"/>
        /// </summary>
        /// <param name="input">The <see cref="List{T}"/> of <see cref="Step3dRowViewModel"/> to transform</param>
        /// <returns>An <see cref="List{ElementDefinition}"/> as the top level <see cref="Thing"/> with changes</returns>
        public override (IEnumerable<ElementDefinition>, IEnumerable<Step3dTargetSourceParameter>) Transform (List<Step3dRowViewModel> input)
        {
            try
            {
                this.idCorrespondences = AppContainer.Container.Resolve<IDstController>().IdCorrespondences;
                
                this.targetSourceParameters = new List<Step3dTargetSourceParameter>();

                this.owner = this.hubController.CurrentDomainOfExpertise;

                foreach (var part in input)
                {
                    // Transformation is as following:
                    // - The STEP part information is stored in a ComposedParameterType
                    // - Having parameter for:
                    //   + Product Definition (name, id, type)
                    //   + Assembly Usage, or Relation (label, id)
                    //   + GUII of the file in the DomainFileStore (known only at Transfer time)
                    //

                    this.dstElementName = part.Name;
                    this.dstParameterName = $"{part.Name} 3D Geometry";

                    if (part.SelectedElementUsages.Any())
                    {
                        this.UpdateValueSetsFromElementUsage(part);
                    }
                    else
                    {
                        if (part.SelectedElementDefinition is null)
                        {
                            part.SelectedElementDefinition = this.Bake<ElementDefinition>(x =>
                            {
                                x.Name = this.dstElementName;
                                x.ShortName = this.dstElementName.Replace(" ", String.Empty);
                                x.Owner = this.owner;
                                x.Container = this.hubController.OpenIteration;
                            });
                        }

                        this.AddsValueSetToTheSelectectedParameter(part);
                        this.AddToExternalIdentifierMap(part.SelectedElementDefinition.Iid, this.dstElementName);
                    }
                }

                // When changes can be also performed in other things
                // (i.e. EU, Parameters, etc.) only the top thing in the 
                // hierarchy is returned, the update will call
                // CreateOrUpdate for all its related things.
                return (input.Select(x => x.SelectedElementDefinition), this.targetSourceParameters);
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Updates the parameters overrides from the selected <see cref="ElementUsage"/>s
        /// </summary>
        /// <param name="part">The current <see cref="Step3dRowViewModel"/></param>
        private void UpdateValueSetsFromElementUsage(Step3dRowViewModel part)
        {
            foreach (var elementUsage in part.SelectedElementUsages)
            {
                // TODO: if the expected parameter is not Overridedable?

                foreach (var parameter in elementUsage.ParameterOverride
                    .Where(x => x.ParameterType is CompoundParameterType parameterType
                        && parameterType.Name.StartsWith("step")))
                    //.Where(x => x.ParameterType is CompoundParameterType parameterType 
                    //            && parameterType.Component.Count == 2 
                    //            && parameterType.Component.SingleOrDefault(x => x.ParameterType is DateTimeParameterType) != null))
                {
                    this.UpdateValueSet(part, parameter);
                    this.AddToExternalIdentifierMap(parameter.Iid, this.dstParameterName);
                }

                this.AddToExternalIdentifierMap(elementUsage.Iid, this.dstElementName);
            }
        }

        /// <summary>
        /// Adds the selected values to the corresponding valueset of the destination parameter
        /// </summary>
        /// <param name="part">The input variable</param>
        private void AddsValueSetToTheSelectectedParameter(Step3dRowViewModel part)
        {
            if (part.SelectedParameter is null)
            {
                if (part.SelectedParameterType is null)
                {
                    if (this.hubController.Session.OpenReferenceDataLibraries
                        .SelectMany(x => x.QueryParameterTypesFromChainOfRdls())
                        .FirstOrDefault(x => x.ShortName == "step_geo") is CompoundParameterType parameterType)
                    {
                        part.SelectedParameterType = parameterType;
                    }
                    else
                    {
                        part.SelectedParameterType = this.CreateCompoundParameterTypeForSte3DGeometry();
                    }
                }

                part.SelectedParameter = this.Bake<Parameter>(x =>
                {
                    x.ParameterType = part.SelectedParameterType;
                    x.Owner = this.owner;
                    x.Container = this.hubController.OpenIteration;
                });
                
                var valueSet = this.Bake<ParameterValueSet>(x =>
                {
                    x.Container = part.SelectedParameter;
                });

                part.SelectedParameter.ValueSet.Add(valueSet);
                part.SelectedElementDefinition.Parameter.Add(part.SelectedParameter);
            }
            
            this.UpdateValueSet(part, part.SelectedParameter);
        }

        /// <summary>
        /// Creates the <see cref="CompoundParameterType"/> for time tagged values
        /// </summary>
        /// <returns>A <see cref="CompoundParameterType"/></returns>
        private CompoundParameterType CreateCompoundParameterTypeForSte3DGeometry()
        {
            // NOTE: this parameter type actually should already exists (DST check at connection)
            return this.Bake<CompoundParameterType>(x =>
            {
                //string STEP_ID_UNIT_NAME = "step id";
                //string STEP_ID_NAME = "step id";
                //string STEP_LABEL_NAME = "step label";
                //string STEP_FILE_REF_NAME = "step file reference";
                string STEP_GEOMETRY_NAME = "step geometry";

                x.ShortName = STEP_GEOMETRY_NAME;
                x.ShortName = "step_geo";
                x.Symbol = "-";

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "name";
                        p.ParameterType = this.Bake<TextParameterType>();
                    }));

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "id";
                        p.ParameterType = this.Bake<SimpleQuantityKind>(
                            d =>
                            {
                                d.Symbol = "#";
                                //d.DefaultScale = ?
                                //d.PossibleScale = new List<MeasurementScale> { ? }
                            }
                            );
                    }));
            });
        }

        /// <summary>
        /// Initializes a new <see cref="Thing"/> of type <typeparamref name="TThing"/>
        /// </summary>
        /// <typeparam name="TThing">The <see cref="Type"/> from which the constructor is invoked</typeparam>
        /// <returns>A <typeparamref name="TThing"/> instance</returns>
        private TThing Bake<TThing>(Action<TThing> initialize = null) where TThing : Thing, new()
        {
            // TODO: take care, Session will not be public
            var tThingInstance = Activator.CreateInstance(typeof(TThing), Guid.Empty /*Guid.NewGuid()*/, this.hubController.Session.Assembler.Cache, new Uri(this.hubController.Session.DataSourceUri)) as TThing;
            initialize?.Invoke(tThingInstance);
            return tThingInstance;
        }

        /// <summary>
        /// Updates the correct value set
        /// </summary>
        /// <param name="variable">The <see cref="VariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Parameter"/></param>
        private void UpdateValueSet(Step3dRowViewModel variable, ParameterBase parameter)
        {
            IValueSet valueSet;

            if (parameter.StateDependence != null && variable.SelectedActualFiniteState is { } actualFiniteState)
            {
                // QUESTION: are those valuesets correctly created... it requires new API maybe?
                valueSet = parameter.ValueSets.Last(x => x.ActualState == actualFiniteState);
            }
            else
            {
                valueSet = parameter.ValueSets.LastOrDefault();
                
                //TODO: check new code from N.S. using the new API
                if (valueSet is null)
                {
                    switch (parameter)
                    {
                        case ParameterOverride parameterOverride:
                            valueSet = this.Bake<ParameterOverrideValueSet>();
                            parameterOverride.ValueSet.Add((ParameterOverrideValueSet)valueSet);
                            break;
                        case Parameter parameterBase:
                            valueSet = this.Bake<ParameterValueSet>();
                            parameterBase.ValueSet.Add((ParameterValueSet)valueSet);
                            break;
                    }
                }
            }

            this.UpdateValueSet(variable, parameter, (ParameterValueSetBase)valueSet);
        }

        /// <summary>
        /// Updates the Computed <see cref="ValueArray{T}"/> of specified <see cref="ParameterValueSetBase"/>
        /// </summary>
        /// <param name="part">The <see cref="Step3dRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Thing"/> <see cref="Parameter"/> or <see cref="ParameterOverride"/></param>
        /// <param name="valueSet">The <see cref="ParameterValueSetBase"/></param>
        private void UpdateValueSet(Step3dRowViewModel part, Thing parameter, ParameterValueSetBase valueSet)
        {
            var valuearray = valueSet.Computed;

            ParameterBase paramBase = (ParameterBase)parameter;
            var paramType = paramBase.ParameterType;

            if (paramType is CompoundParameterType p)
            {
                // Component is an OrderedItemList, and the order could be 
                // changed externally, then do the set value based on 
                // component's name
                int index = 0;
                foreach (ParameterTypeComponent component in p.Component)
                {
                    switch (component.ShortName)
                    {
                        case "name": valuearray[index++] = $"{part.Name}"; break;

                        case "id": valuearray[index++] = $"{part.StepId}"; break;
                        
                        case "rep_type": valuearray[index++] = $"{part.RepresentationType}"; break;
                        
                        case "assembly_label": valuearray[index++] = $"{part.RelationLabel}"; break;
                        
                        case "assembly_id": valuearray[index++] = $"{part.RelationId}"; break;

                        case "source":
                        {
                            // NOTE: FileRevision.Iid will be known at Transfer time
                            this.targetSourceParameters.Add(new Step3dTargetSourceParameter(valuearray, index));
                            valuearray[index++] = "";
                        }
                        break;
                            
                        default:
                        break;
                    }
                }
            }

            //this.AddToExternalIdentifierMap(parameter.Iid, this.dstParameterName);
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="externalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        private void AddToExternalIdentifierMap(Guid internalId, string externalId)
        {
            // OLD:
            //var identifierMap = this.Bake<ExternalIdentifierMap>(x =>
            //{
            //    x.ExternalToolName = typeof(Step3dPartToElementDefinitionRule).Assembly.GetName().Name;
            //    x.Container = this.hubController.OpenIteration;
            //});
            //
            //var idCorrespondence = this.Bake<IdCorrespondence>(x =>
            //{
            //    x.ExternalId = externalId;
            //    x.InternalThing = internalId;
            //});
            //
            //identifierMap.Correspondence.Add(idCorrespondence);
            //
            //this.externalIdentifierMap.Add(identifierMap);
        }
    }
}
