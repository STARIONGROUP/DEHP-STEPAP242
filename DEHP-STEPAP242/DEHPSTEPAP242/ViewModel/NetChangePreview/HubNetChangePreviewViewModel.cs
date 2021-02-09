
namespace DEHPSTEPAP242.ViewModel.NetChangePreview
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using CDP4Dal;

    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.NetChangePreview;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Interfaces;

    using ReactiveUI;


    /// <summary>
    /// The <see cref="HubNetChangePreviewViewModel"/> is the view model 
    /// for the Net Change Preview of the 10-25 data source from 
    /// mappings of STEP-AP242 parts.
    /// </summary>
    class HubNetChangePreviewViewModel : NetChangePreviewViewModel, IHubNetChangePreviewViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel" /> class.
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="hubController">The <see cref="T:DEHPCommon.HubController.Interfaces.IHubController" /></param>
        /// <param name="objectBrowserTreeSelectorService">The <see cref="T:DEHPCommon.Services.ObjectBrowserTreeSelectorService.IObjectBrowserTreeSelectorService" /></param>
        public HubNetChangePreviewViewModel(IDstController dstController, IHubController hubController, IObjectBrowserTreeSelectorService objectBrowserTreeSelectorService) 
            : base(hubController, objectBrowserTreeSelectorService)
        {
            this.dstController = dstController;

            CDPMessageBus.Current.Listen<UpdateObjectBrowserTreeEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.UpdateTree(x.Reset));
        }

        /// <summary>
        /// Updates the tree
        /// </summary>
        /// <param name="shouldReset">A value indicating whether the tree should remove the element in preview</param>
        public void UpdateTree(bool shouldReset)
        {
            if (shouldReset)
            {
                this.Reload();
            }
            else
            {
                this.IsBusy = true;
                this.ComputeValues();
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Computes the old values for each <see cref="P:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel.Things" />
        /// </summary>
        public override void ComputeValues()
        {
            foreach (var iterationRow in this.Things.OfType<ElementDefinitionsBrowserViewModel>())
            {
                foreach (var thing in this.dstController.MapResult)
                {
                    var elementToUpdate = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                        .FirstOrDefault(x => x.Thing.Iid == thing.Iid);

                    if (elementToUpdate is { })
                    {
                        if (!thing.Parameter.All(p => elementToUpdate.Thing.Parameter.Any(x => x.Iid == p.Iid)))
                        {
                            thing.Parameter.AddRange(elementToUpdate.Thing.Parameter.Where(x => thing.Parameter.All(p => p.Iid != x.Iid)));
                        }

                        foreach (var parameterOrOverrideBaseRowViewModel in elementToUpdate.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>())
                        {
                            parameterOrOverrideBaseRowViewModel.SetProperties();
                        }

                        CDPMessageBus.Current.SendMessage(new HighlightEvent(elementToUpdate.Thing), elementToUpdate.Thing);

                        elementToUpdate.ExpandAllRows();
                        elementToUpdate.UpdateThing(thing);
                        elementToUpdate.UpdateChildren();
                    }
                    else
                    {
                        iterationRow.ContainedRows.Add(new ElementDefinitionRowViewModel(thing, this.HubController.CurrentDomainOfExpertise, this.HubController.Session, iterationRow));
                        CDPMessageBus.Current.SendMessage(new HighlightEvent(thing), thing);
                    }

                    foreach (var elementUsage in thing.ContainedElement)
                    {
                        var elementUsageToUpdate = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                            .SelectMany(x => x.ContainedRows.OfType<ElementUsageRowViewModel>())
                            .FirstOrDefault(x => x.Thing.Iid == elementUsage.Iid);

                        if (elementUsageToUpdate is null)
                        {
                            continue;
                        }

                        if (!elementUsage.ParameterOverride.All(p => elementUsageToUpdate.Thing.ParameterOverride.Any(x => x.Iid == p.Iid)))
                        {
                            elementUsage.ParameterOverride.AddRange(elementUsageToUpdate.Thing.ParameterOverride.Where(x => thing.Parameter.All(p => p.Iid != x.Iid)));
                        }

                        foreach (var parameterOrOverrideBaseRowViewModel in elementUsageToUpdate.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>())
                        {
                            parameterOrOverrideBaseRowViewModel.SetProperties();
                        }

                        CDPMessageBus.Current.SendMessage(new ElementUsageHighlightEvent(elementUsageToUpdate.Thing.ElementDefinition), elementUsageToUpdate.Thing);

                        elementUsageToUpdate.ExpandAllRows();
                        elementUsageToUpdate.UpdateThing(elementUsage);
                        elementUsageToUpdate.UpdateChildren();
                    }
                }
            }
        }

        /// <summary>
        /// Not available for the net change preview panel
        /// </summary>
        public override void PopulateContextMenu()
        {
            this.ContextMenu.Clear();
        }
    }
}
