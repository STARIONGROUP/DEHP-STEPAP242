
namespace DEHPSTEPAP242.ViewModel
{
	using System;
	using System.Collections.Generic;
	using ReactiveUI;

	using DEHPSTEPAP242.DstController;
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
	public class DstObjectBrowserViewModel : ReactiveObject, IDstObjectBrowserViewModel
	{
		#region Private Members

		private IDstController dstController;

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
					var node = new Step3DPartTreeNode(p) { ID = nextID++ };
					entries.Add(node);

					AddSubTree(entries, node, ref nextID);
				}
			}

			Step3DHLR = entries;
			IsBusy = false;
		}

		#endregion

		#region Constructor

		public DstObjectBrowserViewModel(IDstController dstController)
		{
			this.dstController = dstController;
			this.WhenAnyValue(x => x.dstController.IsLoading).Subscribe(_ => this.UpdateHLR());
		}

		#endregion

		#region Private Methods

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

				var node = new Step3DPartTreeNode(child) { ID = nextID++, ParentID = parent.ID, RelationLabel = relation.id };
				entries.Add(node);

				AddSubTree(entries, node, ref nextID);
			}
		}

		#endregion
	}
}
