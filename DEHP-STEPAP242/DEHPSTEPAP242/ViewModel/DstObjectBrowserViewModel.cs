
namespace DEHPSTEPAP242.ViewModel
{
	using System.Collections.Generic;
	using ReactiveUI;

	using DEHPSTEPAP242.ViewModel.Interfaces;
	using STEP3DAdapter;

	/// <summary>
	/// The <see cref="DstObjectBrowserViewModel"/> is the view model 
	/// of the <see cref="DstObjectBrowser"/> and provides the 
	/// High Level Representation (aka HLR) of a STEP-AP242 file.
	/// 
	/// Information is provided as a Self-Referential Data Source.
	/// 
	/// <seealso cref="Step3DPartTreeNode"/>
	/// </summary>
	public class DstObjectBrowserViewModel : ReactiveObject, IDstObjectBrowserViewModel
	{
		/// <summary>
		/// Self-referential data source content.
		/// </summary>
		private List<Step3DPartTreeNode> step3DHLR;

		private STEP3D_Part[] parts;
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
		private readonly Dictionary<int, List<int>> partChildren = new Dictionary<int, List<int>>();

		/// <summary>
		/// Keep track of Parts used as parent of an Assembly.
		/// </summary>
		private readonly HashSet<int> relatedParts = new HashSet<int>();

		/// <summary>
		/// Keep track of Parts used as childs of an Assembly.
		/// </summary>
		private readonly HashSet<int> relatingParts = new HashSet<int>();

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
		/// <param name="parts">List of geometric parts</param>
		/// <param name="relations">List of part relations defining instances in the tree composition</param>
		internal void UpdateHLR(STEP3D_Part[] parts, STEP3D_PartRelation[] relations)
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

			this.parts = parts;
			this.relations = relations;

			InitializeAuxiliaryData();

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
		}

		/// <summary>
		/// Fill the auxiliary HasSet/Dictionary to speedup the tree construction.
		/// </summary>
		/// <param name="parts"></param>
		/// <param name="relations"></param>
		private void InitializeAuxiliaryData()
		{
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
		/// Geth Children of a part.
		/// </summary>
		/// <param name="partId">Relating Part (parent)</param>
		/// <returns>List of Part id related to</returns>
		private List<int> FindChildren(int partId)
		{
			if (partChildren.ContainsKey(partId) == false)
			{
				// Create cached list the first time is required
				var ids = new List<int>();

				foreach (var r in relations)
				{
					if (r.relating_id == partId)
					{
						ids.Add(r.related_id);
					}
				}

				partChildren.Add(partId, ids);
			}

			return partChildren[partId];
		}

		/// <summary>
		/// Find the Part object from its StepId.
		/// </summary>
		/// <param name="partId">Step Id of the part</param>
		/// <returns>A <see cref="STEP3D_Part"/></returns>
		private STEP3D_Part FindPart(int partId)
		{
			return idToPartMap[partId];
		}

		/// <summary>
		/// Find the Relation of two parts.
		/// 
		/// Note: it is not expected to have duplicated relations 
		/// for the same pair (relating, related).
		/// </summary>
		/// <param name="relatingId"></param>
		/// <param name="relatedId"></param>
		/// <returns>A <see cref="STEP3D_PartRelation"/> or null if fails</returns>
		private STEP3D_PartRelation FindRelation(int relatingId, int relatedId)
		{
			foreach (var r in relations)
			{
				if (r.relating_id == relatingId && r.related_id == relatedId)
				{
					return r;
				}
			}

			return null;
		}

		/// <summary>
		/// Add children of a tree node.
		/// </summary>
		/// <param name="entries">tree container to fill</param>
		/// <param name="e">parent node</param>
		/// <param name="nextID">global tree ID for next creation operation</param>
		private void AddSubTree(List<Step3DPartTreeNode> entries, Step3DPartTreeNode e, ref int nextID )
		{
			var childrenIds = FindChildren(e.StepId);

			foreach(var id in childrenIds)
			{
				var child = FindPart(id);
				var relation = FindRelation(e.StepId, child.stepId);

				var node = new Step3DPartTreeNode(child) { ID = nextID++, ParentID = e.ID, RelationLabel = relation?.id };
				entries.Add(node);

				AddSubTree(entries, node, ref nextID);
			}
		}
	}
}
