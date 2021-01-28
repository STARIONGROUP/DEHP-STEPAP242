
namespace DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder
{
    using ReactiveUI;

    using DEHPSTEPAP242.ViewModel.Rows;

    using STEP3DAdapter;
    using System.Collections.Generic;

    /// <summary>
    /// Self-referential data source content.
    /// 
    /// Using the following service columns:
    /// - Key Field --> Step3DPartTreeNode.ID
    /// - Parent Field --> Step3DPartTreeNode.ParentID
    /// </summary>
    public class HLRBuilder : IHLRBuilder
    {
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
        /// <seealso cref="FindPart(int)"/>
        /// </summary>
        private readonly Dictionary<int, STEP3D_Part> idToPartMap = new Dictionary<int, STEP3D_Part>();

        /// <summary>
        /// Helper structure to speedup tree searches.
        /// <seealso cref="InitializeAuxiliaryData"/>
        private readonly Dictionary<int, STEP3D_PartRelation> idToRelationMap = new Dictionary<int, STEP3D_PartRelation>();

        /// <summary>
        /// Helper structure to speedup tree searches.
        /// <seealso cref="FindChildren(int)"/>
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
        /// Creates the High Level Representation (HLR) View Model for STEP AP242 file
        /// </summary>
        /// <remarks>
        /// HLR Tree construction:
        /// 
        /// Each Part could appears many times
        /// * As used in an Assembly
        /// * As that Assembly is also used in other Assemblies
        ///
        /// Assemblies (roots) trigger recursivity
        /// Parts (leaves) are processed normally
        /// Parts not referenced as target of any Assembly belongs to the main Root
        ///
        /// Each ParentID is associated to a PartRelation which must be stored
        /// in some place to retrieve the information of the specific association.
        ///
        /// The global identification of a Part instance is the full path of IDs.
        /// </remarks>
        public List<Step3dRowViewModel> CreateHLR(STEP3DFile step3d)
        {
            var entries = new List<Step3dRowViewModel>();

            if (step3d is null)
            {
                InitializeAuxiliaryData(step3d.Parts, step3d.Relations);
            }
            else
            {
                InitializeAuxiliaryData(new STEP3D_Part[0], new STEP3D_PartRelation[0]);
            }

            int nextID = 1;

            foreach (var p in this.parts)
            {
                if (IsIsolatedPart(p))
                {
                    // Add to the maint Root
                    var node = new Step3dRowViewModel(p, null) { ID = nextID++ };
                    entries.Add(node);

                    AddSubTree(entries, node, ref nextID);
                }
            }

            return entries;
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
        private void AddSubTree(ICollection<Step3dRowViewModel> entries, Step3dRowViewModel parent, ref int nextID)
        {
            var children = FindChildren(parent.StepId);

            foreach (var cr in children)
            {
                var child = cr.Item1;
                var relation = cr.Item2;

                var node = new Step3dRowViewModel(child, relation)
                {
                    ID = nextID++,
                    ParentID = parent.ID
                };

                entries.Add(node);

                AddSubTree(entries, node, ref nextID);
            }
        }
    }
}
