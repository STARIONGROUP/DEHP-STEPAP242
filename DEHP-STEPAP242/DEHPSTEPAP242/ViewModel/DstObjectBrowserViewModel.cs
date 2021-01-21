
namespace DEHPSTEPAP242.ViewModel
{
	using System;
	using System.Collections.Generic;

	using ReactiveUI;

	using CDP4Common.CommonData;
	using DEHPCommon.Enumerators;
	using DEHPCommon.HubController.Interfaces;
	using DEHPCommon.Services.NavigationService;
	using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
	using DEHPCommon.UserInterfaces.ViewModels;

	using DEHPSTEPAP242.DstController;
	using DEHPSTEPAP242.Views.Dialogs;
	using DEHPSTEPAP242.ViewModel.Interfaces;

	using STEP3DAdapter;

	/// <summary>
	/// The <see cref="DstObjectBrowserViewModel"/> is the view model 
	/// of the <see cref="DstObjectBrowser"/> and provides the 
	/// High Level Representation (aka HLR) of a STEP-AP242 file.
	/// 
	/// Information is provided as a Self-Referential Data Source.
	/// 
	/// To represent data in a tree structure, the data source should contain the following fields:
	/// - Key Field: This field should contain unique values used to identify nodes.
	/// - Parent Field: This field should contain values that indicate parent nodes.
	/// 
	/// <seealso cref="Step3DPartTreeNode"/>
	/// </summary>
	public class DstObjectBrowserViewModel : ReactiveObject, IDstObjectBrowserViewModel, IHaveContextMenuViewModel
	{
		#region Private Interface References

		/// <summary>
		/// The <see cref="IHubController"/>
		/// </summary>
		private readonly IHubController hubController;

		/// <summary>
		/// The <see cref="IDstController"/>
		/// </summary>
		private readonly IDstController dstController;

		/// <summary>
		/// The <see cref="INavigationService"/>
		/// </summary>
		private readonly INavigationService navigationService;

		#endregion Private Interface References

		#region Private Members

		/// <summary>
		/// List of geometric parts.
		/// 
		/// A part could be the container of parts.
		/// </summary>
		private STEP3D_Part[] parts;

		/// <summary>
		/// List of relations between geometric parts
		/// </summary>
		private STEP3D_PartRelation[] relations;

		/// <summary>
		/// Helper structure to speedup tree searches.
		/// See FindPart().
		/// </summary>
		private readonly Dictionary<int, STEP3D_Part> idToPartMap = new Dictionary<int, STEP3D_Part>();

		/// <summary>
		/// Helper structure to speedup tree searches.
		/// See FindRelation().
		private readonly Dictionary<int, STEP3D_PartRelation> idToRelationMap = new Dictionary<int, STEP3D_PartRelation>();

		/// <summary>
		/// Helper structure to speedup tree searches.
		/// See FindChildren().
		/// </summary>
		private readonly Dictionary<int, List<(STEP3D_Part, STEP3D_PartRelation)>> partChildren = new Dictionary<int, List<(STEP3D_Part, STEP3D_PartRelation)>>();

		/// <summary>
		/// Keep track of Parts used as parent of an Assembly.
		/// </summary>
		private readonly HashSet<int> relatedParts = new HashSet<int>();

		/// <summary>
		/// Keep track of Parts used as childs of an Assembly.
		/// </summary>
		private readonly HashSet<int> relatingParts = new HashSet<int>();

		/// <summary>
		/// Self-referential data source content.
		/// 
		/// Using the following service columns:
		/// - Key Field --> Step3DPartTreeNode.ID
		/// - Parent Field --> Step3DPartTreeNode.ParentID
		/// </summary>

		#endregion

		#region IDstObjectBrowserViewModel interface

		/// <summary>
		/// Backing field for <see cref="IsBusy"/>
		/// </summary>
		private bool isBusy;

		/// <summary>
		/// Gets or sets the assert indicating whether the view is busy
		/// </summary>
		public bool IsBusy
		{
			get => this.isBusy;
			set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
		}

		/// <summary>
		/// Backing field for <see cref="Step3DHLR"/>
		/// </summary>
		private List<Step3DPartTreeNode> step3DHLR = new List<Step3DPartTreeNode>();

		/// <summary>
		/// Gets or sets the Step3D High Level Representation structure.
		/// </summary>
		public List<Step3DPartTreeNode> Step3DHLR
		{
			get => this.step3DHLR;
			private set => this.RaiseAndSetIfChanged(ref this.step3DHLR, value);
		}

		/// <summary>
		/// Backing field for <see cref="SelectedPart"/>
		/// </summary>
		private Step3DPartTreeNode selectedPart;

		/// <summary>
		/// Gets or sets the selected row that represents a <see cref="ReferenceDescription"/>
		/// </summary>
		public Step3DPartTreeNode SelectedPart
		{
			get => this.selectedPart;
			set => this.RaiseAndSetIfChanged(ref this.selectedPart, value);
		}

		/// <summary>
		/// Create the HLR tree from the Parts/Relations.
		/// 
		/// The HLR is a Self-Referential Data Source. In this approach
		/// each item in the list has a pair values describing the tree link.
		/// they are ID and ParentID.
		/// 
		/// The ID and ParentID are assigned in a way they are different 
		/// at each level, enabling that sub-trees can be repeated at
		/// different levels.
		/// 
		/// Any <see cref="STEP3D_Part"/> item without <see cref="STEP3D_PartRelation"/> 
		/// defining a specific instance, it will placed as child of the main root item.
		/// </summary>
		/// <param name="step3d">A <see cref="STEP3DFile"/> instance</param>
		public void UpdateHLR()
		{
			// HLR Tree construction:
			// Each Part could appears many times
			// * As used in an Assembly
			// * As that Assembly is also used in other Assemblies
			//
			// Assemblies (roots) trigger recursivity
			// Parts (leaves) are processed normally
			// Parts not referenced as target of any Assembly belongs to the main Root
			//
			// Each ParentID is associated to a PartRelation which must be stored
			// in some place to retrieve the information of the specific association.
			//
			// The global identification of a Part instance is the full path of IDs.

			if (dstController.IsLoading)
			{
				IsBusy = true;
				return;
			}

			STEP3DFile step3d = dstController.Step3DFile;

			if (step3d != null)
			{
				InitializeAuxiliaryData(step3d.Parts, step3d.Relations);
			}
			else
			{
				InitializeAuxiliaryData(new STEP3D_Part[0], new STEP3D_PartRelation[0]);
			}

			var entries = new List<Step3DPartTreeNode>();

			int nextID = 1;

			foreach (var p in this.parts)
			{
				if (IsIsolatedPart(p))
				{
					// Add to the maint Root
					var node = new Step3DPartTreeNode(p, null) { ID = nextID++ };
					entries.Add(node);

					AddSubTree(entries, node, ref nextID);
				}
			}

			SelectedPart = null;
			Step3DHLR = entries;

			IsBusy = false;
		}

		#endregion

		#region IHaveContextMenuViewModel interface

		/// <summary>
		/// Gets the Context Menu for this browser
		/// </summary>
		public ReactiveList<ContextMenuItemViewModel> ContextMenu { get; } = new ReactiveList<ContextMenuItemViewModel>();

		/// <summary>
		/// Gets the command that allows to map the selected things
		/// </summary>
		public ReactiveCommand<object> MapCommand { get; set; }

		/// <summary>
		/// Populate the context menu for this browser
		/// </summary>
		public void PopulateContextMenu()
		{
			ContextMenu.Clear();

			if (SelectedPart is null)
			{
			    return;
			}

			ContextMenu.Add(new ContextMenuItemViewModel(
					"Map selection", "",
					MapCommand,
					MenuItemKind.Export,
					ClassKind.NotThing)
				);
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new <see cref="DstObjectBrowserViewModel"/>
		/// </summary>
		/// <param name="dstController">The <see cref="IDstController"/></param>
		/// <param name="navigationService">The <see cref="INavigationService"/></param>
		/// <param name="hubController">The <see cref="IHubController"/></param>
		public DstObjectBrowserViewModel(IDstController dstController, INavigationService navigationService, IHubController hubController)
		{
			this.dstController = dstController;
			this.navigationService = navigationService;
			this.hubController = hubController;

			this.WhenAnyValue(vm => vm.dstController.IsLoading).Subscribe(_ => this.UpdateHLR());

			this.WhenAnyValue(vm => vm.SelectedPart)
				.Subscribe(_ => this.PopulateContextMenu());

			InitializeCommands();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Initializes the <see cref="ICommand"/> of this view model
		/// </summary>
		private void InitializeCommands()
		{
			//var canMap = this.WhenAny(
			//	vm => vm.hubController.OpenIteration,
			//	vm => vm.dstController.MappingDirection,
			//    (iteration, mappingDirection) =>
			//	iteration.Value != null && mappingDirection.Value is MappingDirection.FromDstToHub);
			//
			//MapCommand = ReactiveCommand.Create(canMap);
			MapCommand = ReactiveCommand.Create();
			MapCommand.Subscribe(_ => this.MapCommandExecute());

			/*
			var canMap = this.WhenAny(
				//vm => vm.SelectedThing,
				//vm => vm.SelectedThings.CountChanged,
				vm => vm.hubController.OpenIteration,
				vm => vm.dstController.MappingDirection,
				//(selected, selection, iteration, mappingDirection) =>
				//	iteration.Value != null && (selected.Value != null || this.SelectedThings.Any()) && mappingDirection.Value is MappingDirection.FromDstToHub);

			this.MapCommand = ReactiveCommand.Create(canMap);
			this.MapCommand.Subscribe(_ => this.MapCommandExecute());
			*/
		}

		/// <summary>
		/// Executes the <see cref="MapCommand"/>
		/// </summary>
		private void MapCommandExecute()
		{
			navigationService.ShowDialog<DstLoadFile>();

			//var viewModel = AppContainer.Container.Resolve<IMappingConfigurationDialogViewModel>();
			//viewModel.Variables.AddRange(this.SelectedThings);
			//this.navigationService.ShowDialog<MappingConfigurationDialog, IMappingConfigurationDialogViewModel>(viewModel);
		}

		/// <summary>
		/// Fill the auxiliary HasSet/Dictionary to speedup the tree construction.
		/// </summary>
		/// <param name="parts">List of geometric parts</param>
		/// <param name="relations">List of part relations defining instances in the tree composition</param>
		private void InitializeAuxiliaryData(STEP3D_Part[] parts, STEP3D_PartRelation[] relations)
		{
			this.parts = parts;
			this.relations = relations;

			idToPartMap.Clear();
			idToRelationMap.Clear();
			partChildren.Clear();  // Constructed at FindChildren() call
			relatingParts.Clear();
			relatedParts.Clear();

			// Fill auxiliary helper structures

			foreach (var p in parts)
			{
				idToPartMap.Add(p.stepId, p);
			}

			foreach (var r in relations)
			{
				idToRelationMap.Add(r.stepId, r);

				relatingParts.Add(r.relating_id);
				relatedParts.Add(r.related_id);
			}
		}

		/// <summary>
		/// Verify if a Part does not belong to any other Part
		/// </summary>
		/// <param name="part"></param>
		/// <returns>True is not used as related part</returns>
		private bool IsIsolatedPart(STEP3D_Part part)
		{
			return relatedParts.Contains(part.stepId) == false;
		}

		/// <summary>
		/// Gets Children of a Part.
		/// </summary>
		/// <param name="parentId">StepId of a relating Part (parent)</param>
		/// <returns>List of child Part and PartRelation generating the instance</returns>
		private List<(STEP3D_Part, STEP3D_PartRelation)> FindChildren(int parentId)
		{
			if (partChildren.ContainsKey(parentId) == false)
			{
				// Create cached list the first time is required
				var childrenRelation = new List<(STEP3D_Part, STEP3D_PartRelation)>();

				foreach (var r in relations)
				{
					if (r.relating_id == parentId)
					{
						childrenRelation.Add((FindPart(r.related_id), r));
					}
				}

				partChildren.Add(parentId, childrenRelation);
			}

			return partChildren[parentId];
		}

		/// <summary>
		/// Finds the Part object from its StepId.
		/// </summary>
		/// <param name="partId">Step Id of a Part</param>
		/// <returns>A <see cref="STEP3D_Part"/></returns>
		private STEP3D_Part FindPart(int partId)
		{
			return idToPartMap[partId];
		}

		/// <summary>
		/// Adds children of a tree node.
		/// </summary>
		/// <param name="entries">tree container to fill</param>
		/// <param name="parent">parent node</param>
		/// <param name="nextID">global tree ID for next creation operation</param>
		private void AddSubTree(List<Step3DPartTreeNode> entries, Step3DPartTreeNode parent, ref int nextID )
		{
			var children = FindChildren(parent.StepId);

			foreach(var cr in children)
			{
				var child = cr.Item1;
				var relation = cr.Item2;

				var node = new Step3DPartTreeNode(child, relation) 
				{ 
					ID = nextID++, 
					ParentID = parent.ID 
				};

				entries.Add(node);

				AddSubTree(entries, node, ref nextID);
			}
		}

		#endregion
	}
}
