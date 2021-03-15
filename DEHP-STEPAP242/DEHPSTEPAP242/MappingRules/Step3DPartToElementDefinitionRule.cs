
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
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.ViewModel.Rows;

    using NLog;

    /// <summary>
    /// Helper class which keeps a reference to the <see cref="ValueArray{string}"/> 
    /// that needs to me updated with the new <see cref="FileRevision"/> of the source
    /// STEP 3D file in the Hub.
    /// </summary>
    public class Step3DTargetSourceParameter
    {
        /// <summary>
        /// The <see cref="Step3DRowViewModel"/> originating the change
        /// </summary>
        public readonly Step3DRowViewModel part;

        /// <summary>
        /// The <see cref="ValueArray{string}"/> of the <see cref="IValueSet"/> of interest
        /// </summary>
        private readonly ValueArray<string> values;

        /// <summary>
        /// The index in the <see cref="ValueArray{string}"/> for the <see cref="ParameterTypeComponent"/> corresponding to the "source" field
        /// </summary>
        private readonly int componentIndex;

        public Step3DTargetSourceParameter(Step3DRowViewModel part, ValueArray<string> values, int componentIndex)
        {
            this.part = part;
            this.values = values;
            this.componentIndex = componentIndex;
        }

        /// <summary>
        /// Updates the <see cref="ValueArray{string}"/> associated to the source parameter
        /// </summary>
        /// <param name="fileRevision"></param>
        public void UpdateSource(FileRevision fileRevision)
        {
            this.values[componentIndex] = fileRevision.Iid.ToString();
        }
    }

    /// <summary>
    /// The <see cref="Step3DPartToElementDefinitionRule"/> is a <see cref="IMappingRule"/> 
    /// for the <see cref="MappingEngine"/>.
    /// 
    /// That takes a <see cref="List{T}"/> of <see cref="Step3DRowViewModel"/> as input 
    /// and outputs a E-TM-10-25 <see cref="ElementDefinition"/>.
    /// </summary>
    public class Step3DPartToElementDefinitionRule : MappingRule<List<Step3DRowViewModel>, (List<ElementBase>, List<Step3DTargetSourceParameter>)>
    {
        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private IDstController dstController;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController = AppContainer.Container.Resolve<IHubController>();

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService = AppContainer.Container.Resolve<IDstHubService>();

        /// <summary>
        /// The <see cref="List{ElementBase}>"/> that needs to be updated 
        /// before the transfer to the Hub.
        /// </summary>
        private List<ElementBase> targetSourceElementBase;

        /// <summary>
        /// The <see cref="List{Step3dTargetSourceParameter}>"/> that needs to be updated 
        /// before the transfer to the Hub.
        /// </summary>
        private List<Step3DTargetSourceParameter> targetSourceParameters;

        /// <summary>
        /// The current <see cref="DomainOfExpertise"/>
        /// </summary>
        private DomainOfExpertise owner;

        /// <summary>
        /// Holds the current processing <see cref="Step3DRowViewModel"/> element name
        /// </summary>
        private string dstElementName;

        /// <summary>
        /// Holds the current processing <see cref="Step3DRowViewModel"/> parameter name
        /// </summary>
        private string dstParameterName;

        /// <summary>
        /// Transforms a <see cref="List{T}"/> of <see cref="Step3DRowViewModel"/> into an <see cref="ElementDefinition"/>
        /// </summary>
        /// <param name="input">The <see cref="List{T}"/> of <see cref="Step3DRowViewModel"/> to transform</param>
        /// <returns>An <see cref="List{ElementDefinition}"/> as the top level <see cref="Thing"/> with changes</returns>
        public override (List<ElementBase>, List<Step3DTargetSourceParameter>) Transform (List<Step3DRowViewModel> input)
        {
            try
            {
                this.dstController = AppContainer.Container.Resolve<IDstController>();
                
                this.targetSourceParameters = new List<Step3DTargetSourceParameter>();
                this.targetSourceElementBase = new List<ElementBase>();

                this.owner = this.hubController.CurrentDomainOfExpertise;

                foreach (var part in input)
                {
                    // Transformation is as following:
                    // - The STEP part information is stored in a ComposedParameterType
                    // - Having parameter for:
                    //   + Product Definition (name, id, type)
                    //   + Assembly Usage, or Relation (label, id)
                    //   + Uuid of the file in the DomainFileStore (known only at Transfer time)
                    //


                    this.logger.Info($"Processing MappingRule for: {part.Description}");

                    // Default values
                    this.dstElementName = part.ElementName;
                    this.dstParameterName = part.ParameterName;

                    if (part.SelectedElementUsages.Any())
                    {
                        this.UpdateValueSetsFromElementUsage(part);
                    }
                    else
                    {
                        if (part.SelectedElementDefinition is null)
                        {
                            part.SelectedElementDefinition = this.GetElementDefinition();

                            if (part.SelectedElementDefinition.Iid != Guid.Empty)
                            {
                                // When the ED was automatically selected from the Rule,
                                // set also the expected parameter (if does exist, it rests as null)
                                part.SelectedParameter = part.SelectedElementDefinition.Parameter
                                    .FirstOrDefault(x => this.dstHubService.IsSTEPParameterType(x.ParameterType));
                            }
                        }

                        this.targetSourceElementBase.Add(part.SelectedElementDefinition);

                        this.AddsValueSetToTheSelectectedParameter(part);
                        this.AddToExternalIdentifierMap(part.SelectedElementDefinition.Iid, this.dstElementName);
                    }

                    part.SetMappedStatus();
                }

                // When changes can be also performed in other things
                // (i.e. EU, Parameters, etc.) only the top thing in the 
                // hierarchy is returned, the update will call
                // CreateOrUpdate for all its related things.
                return (this.targetSourceElementBase, this.targetSourceParameters);
            }
            catch (Exception exception)
            {
                this.logger.Error(exception);
                this.logger.Error($"Mapping Step3DRowViewModel failed: {exception.Message}");
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }
        }

        /// <summary>
        /// Updates the parameters overrides from the selected <see cref="ElementUsage"/>s
        /// </summary>
        /// <param name="part">The current <see cref="Step3DRowViewModel"/></param>
        private void UpdateValueSetsFromElementUsage(Step3DRowViewModel part)
        {
            foreach (var elementUsage in part.SelectedElementUsages)
            {
                this.targetSourceElementBase.Add(elementUsage);

                ParameterOverride parameterOverride;

                if (part.SelectedParameter is { } parameter)
                {
                    if (elementUsage.ParameterOverride.FirstOrDefault(x => x.Parameter.Iid == parameter.Iid) is { } existingOverride)
                    {
                        parameterOverride = existingOverride;
                    }
                    else
                    {
                        parameterOverride = this.Bake<ParameterOverride>(x =>
                        {
                            x.Parameter = parameter;
                            //x.ParameterType = parameter.ParameterType;
                            //x.StateDependence = parameter.StateDependence;
                            //x.IsOptionDependent = parameter.IsOptionDependent;
                            x.Owner = this.owner;
                        });

                        var valueSet = this.Bake<ParameterOverrideValueSet>(x =>
                        {
                        });

                        parameterOverride.ValueSet.Add(valueSet);

                        elementUsage.ParameterOverride.Add(parameterOverride);
                    }
                }
                else
                {
                    // No parameter selected, the parameter do not exist the first mapping time
                    parameterOverride = elementUsage.ParameterOverride.FirstOrDefault(x => this.dstHubService.IsSTEPParameterType(x.ParameterType));

                    if (parameterOverride is null &&
                        elementUsage.ElementDefinition.Parameter.FirstOrDefault(x => x.ParameterType.Name == this.dstParameterName) is { } parameterToOverride)
                    {
                        parameterOverride = this.Bake<ParameterOverride>(x =>
                        {
                            x.Parameter = parameterToOverride;
                            //x.ParameterType = parameterToOverride.ParameterType;
                            x.StateDependence = parameterToOverride.StateDependence;
                            x.IsOptionDependent = parameterToOverride.IsOptionDependent;
                            
                            x.Owner = this.owner;
                        });

                        elementUsage.ParameterOverride.Add(parameterOverride);
                    }
                }

                if (parameterOverride is { })
                {
                    this.UpdateValueSet(part, parameterOverride);
                }

                this.AddToExternalIdentifierMap(elementUsage.ElementDefinition.Iid, this.dstElementName);
                this.AddToExternalIdentifierMap(elementUsage.Iid, this.dstElementName);
            }
        }

        /// <summary>
        /// Gets or Creates an <see cref="ElementDefinition"/> if it does not exist yet
        /// </summary>
        /// <returns>An <see cref="ElementDefinition"/>. New <see cref="ElementDefinition"/> is identified as <see cref="Guid.Empty"/></returns>
        private ElementDefinition GetElementDefinition()
        {
            // Check if already exists in the hub
            if (this.hubController.OpenIteration.Element
                .FirstOrDefault(x => x.Name == this.dstElementName) is { } elementDefinition)
            {
                return elementDefinition;
            }

            this.logger.Info($"Creating new ElementDefinition '{this.dstElementName}'");

            return this.Bake<ElementDefinition>(x =>
            {
                x.Name = this.dstElementName;
                x.ShortName = this.dstElementName.Replace(" ", String.Empty);
                x.Owner = this.owner;
                x.Container = this.hubController.OpenIteration;
            });
        }

        /// <summary>
        /// Adds the selected values to the corresponding valueset of the destination parameter
        /// </summary>
        /// <param name="part">The input part</param>
        private void AddsValueSetToTheSelectectedParameter(Step3DRowViewModel part)
        {
            if (part.SelectedParameter is null)
            {
                if (part.SelectedParameterType is null)
                {
                    part.SelectedParameterType = this.GetStep3dGeometryParameterType();
                }

                this.logger.Info($"Creating new Parameter of type '{part.SelectedParameterType.Name}' into ElementDefinition '{part.SelectedElementDefinition.Name}'");

                part.SelectedParameter = this.Bake<Parameter>(x =>
                {
                    x.ParameterType = part.SelectedParameterType;
                    //TODO: ask how to add parameter and make it option dependent --> ParameterOverride
                    x.Owner = this.owner;
                });

                var valueSet = this.Bake<ParameterValueSet>(x =>
                {
                });

                part.SelectedParameter.ValueSet.Add(valueSet);
                part.SelectedElementDefinition.Parameter.Add(part.SelectedParameter);
            }
            
            this.UpdateValueSet(part, part.SelectedParameter);
        }

        /// <summary>
        /// Gets the existing parameter type or creates a new one
        /// </summary>
        /// <returns>A <see cref="ParameterType"/></returns>
        private ParameterType GetStep3dGeometryParameterType()
        {
            var rdl = this.dstHubService.GetReferenceDataLibrary();

            var parameterType = rdl.ParameterType.FirstOrDefault(x => this.dstHubService.IsSTEPParameterType(x));

            if (parameterType is null)
            {
                // NOTE: this should not happen, the DST creates required types at connection time
                this.logger.Warn("STEP Geometry parameter not found, creating a new one!");

                parameterType = this.CreateCompoundParameterTypeForSte3DGeometry();
            }

            return parameterType;
        }

        /// <summary>
        /// Creates the <see cref="CompoundParameterType"/> for time tagged values
        /// </summary>
        /// <returns>A <see cref="CompoundParameterType"/></returns>
        /// <remarks>This method will not be called because all was created at connection time</remarks>
        private CompoundParameterType CreateCompoundParameterTypeForSte3DGeometry()
        {
            this.logger.Warn("STEP Geometry compound parameter should be created by HubDstService instance at Connect time");

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
            var tThingInstance = Activator.CreateInstance(typeof(TThing), Guid.Empty, this.hubController.Session.Assembler.Cache, new Uri(this.hubController.Session.DataSourceUri)) as TThing;
            initialize?.Invoke(tThingInstance);
            return tThingInstance;
        }

        /// <summary>
        /// Updates the correct value set
        /// </summary>
        /// <param name="part">The <see cref="VariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Parameter"/></param>
        private void UpdateValueSet(Step3DRowViewModel part, ParameterBase parameter)
        {
            var valueSet = (ParameterValueSetBase)parameter.QueryParameterBaseValueSet(part.SelectedOption, part.SelectedActualFiniteState);

            this.UpdateComputedValueSet(part, parameter, valueSet);
        }

        /// <summary>
        /// Updates the Computed <see cref="ParameterValueSetBase"/> <see cref="ValueArray{T}"/>
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Thing"/> <see cref="Parameter"/> or <see cref="ParameterOverride"/></param>
        /// <param name="valueSet">The <see cref="ParameterValueSetBase"/></param>
        private void UpdateComputedValueSet(Step3DRowViewModel part, Thing parameter, ParameterValueSetBase valueSet)
        {
            ParameterBase paramBase = (ParameterBase)parameter;
            var paramType = paramBase.ParameterType;

            if (paramType is CompoundParameterType p)
            {
                var valuearray = valueSet.Computed;

                if (valuearray.Count == 0)
                {
                    // New parameter does not contain ValueArray with the expected dimmension,
                    // they are filled by the server side, then it is necessary to create the
                    // expected content here.

                    this.logger.Debug($"Computed ValueArray is empty (expected on new parameters) --> initializing to CompoundParameterType.NumberOfValues={p.NumberOfValues}");

                    var values = new List<string>(p.NumberOfValues);
                    foreach (var i in System.Linq.Enumerable.Range(0, p.NumberOfValues))
                    {
                        values.Add("-");
                    }

                    valueSet.Computed = new ValueArray<string>(values);
                    valueSet.Manual = new ValueArray<string>(values);
                    valueSet.Reference = new ValueArray<string>(values);
                    valueSet.Formula = new ValueArray<string>(values);

                    valuearray = valueSet.Computed;
                }

                UpdateValueArrayForCompoundParameterType(part, p, valuearray);

                valueSet.ValueSwitch = ParameterSwitchKind.COMPUTED;

                this.AddToExternalIdentifierMap(parameter.Iid, this.dstParameterName);

                if (part.SelectedOption is { })
                {
                    this.AddToExternalIdentifierMap(part.SelectedOption.Iid, this.dstParameterName);
                }

                if (part.SelectedActualFiniteState is { })
                {
                    this.AddToExternalIdentifierMap(part.SelectedActualFiniteState.Iid, this.dstParameterName);
                }
            }
        }

        /// <summary>
        /// Update <see cref="CompoundParameterType"/> <see cref="ValueArray{string}"/>
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/></param>
        /// <param name="parameter">The <see cref="CompoundParameterType"/></param>
        /// <param name="valuearray">The <see cref="ValueArray{string}"/></param>
        /// <remarks>
        /// Creates a <seealso cref="Step3DTargetSourceParameter"/> entry when a
        /// <see cref="ParameterTypeComponent"/> named "source" is present in the <paramref name="parameter"/>.
        /// </remarks>
        private void UpdateValueArrayForCompoundParameterType(Step3DRowViewModel part, CompoundParameterType parameter, ValueArray<string> valuearray)
        {
            // Component is an OrderedItemList, and the order could be 
            // changed externally by modifyind the ParameterType definition,
            // then do the set the value based on component's name

            int index = 0;
            foreach (ParameterTypeComponent component in parameter.Component)
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
                        //       store the current index to know which possition corresponds
                        //       to the source (avoid searching it again)
                        this.targetSourceParameters.Add(new Step3DTargetSourceParameter(part, valuearray, index));
                        valuearray[index++] = "";
                    }
                    break;

                    default:
                    break;
                }
            }
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="externalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        private void AddToExternalIdentifierMap(Guid internalId, string externalId)
            => this.dstController.AddToExternalIdentifierMap(internalId, externalId);
    }
}
