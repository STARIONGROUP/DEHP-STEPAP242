
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
    /// The <see cref="Step3dPartToElementDefinitionRule"/> is a <see cref="IMappingRule"/> 
    /// for the <see cref="MappingEngine"/>.
    /// 
    /// That takes a <see cref="List{T}"/> of <see cref="Step3dRowViewModel"/> as input 
    /// and outputs a E-TM-10-25 <see cref="ElementDefinition"/>.
    /// </summary>
    public class Step3dPartToElementDefinitionRule : MappingRule<List<Step3dRowViewModel>, IEnumerable<ElementDefinition>>
    {
        /// <summary>
        /// The current class logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController = AppContainer.Container.Resolve<IHubController>();

        // /// <summary>
        // /// The  collection of <see cref="ExternalIdentifierMap"/>
        // /// </summary>
        // private readonly List<ExternalIdentifierMap> externalIdentifierMap = new List<ExternalIdentifierMap>();

        /// <summary>
        /// Gets the <see cref="idCorrespondences"/>
        /// </summary>
        private List<IdCorrespondence> idCorrespondences;

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
        /// <returns>An <see cref="ElementDefinition"/></returns>
        public override IEnumerable<ElementDefinition> Transform (List<Step3dRowViewModel> input)
        {
            try
            {
                this.idCorrespondences = AppContainer.Container.Resolve<IDstController>().IdCorrespondences;

                this.owner = this.hubController.CurrentDomainOfExpertise;

                foreach (var part in input)
                {
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

                return (input.Select(x => x.SelectedElementDefinition));
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
                        .FirstOrDefault(x => x.Name == "TimeTaggedValue") is CompoundParameterType parameterType)
                    {
                        part.SelectedParameterType = parameterType;
                    }
                    else
                    {
                        part.SelectedParameterType = this.CreateCompoundParameterTypeForEcosimTimetaggedValues();
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
        private CompoundParameterType CreateCompoundParameterTypeForEcosimTimetaggedValues()
        {
            return this.Bake<CompoundParameterType>(x =>
            {
                x.ShortName = "TimeTaggedValue";
                x.Name = "TimeTaggedValue";
                x.Symbol = "ttv";

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "TtvDateTime";
                        p.ParameterType = this.Bake<DateTimeParameterType>();
                    }));

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "TtvValue";
                        p.ParameterType = this.Bake<SimpleQuantityKind>();
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
            var tThingInstance = Activator.CreateInstance(typeof(TThing), Guid.NewGuid(), this.hubController.Session.Assembler.Cache, new Uri(this.hubController.Session.DataSourceUri)) as TThing;
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
                valueSet = parameter.ValueSets.Last(x => x.ActualState == actualFiniteState);
            }
            else
            {
                valueSet = parameter.ValueSets.LastOrDefault();
                
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
        /// Updates the specified value set
        /// </summary>
        /// <param name="variable">The <see cref="Step3dRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Thing"/> <see cref="Parameter"/> or <see cref="ParameterOverride"/></param>
        /// <param name="valueSet">The <see cref="ParameterValueSetBase"/></param>
        private void UpdateValueSet(Step3dRowViewModel variable, Thing parameter, ParameterValueSetBase valueSet)
        {
            //valueSet.Computed = new ValueArray<string>(
            //    variable.SelectedValues.Select(
            //        x => FormattableString.Invariant($"{x.Value}")));
            //
            this.AddToExternalIdentifierMap(parameter.Iid, this.dstParameterName);
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
