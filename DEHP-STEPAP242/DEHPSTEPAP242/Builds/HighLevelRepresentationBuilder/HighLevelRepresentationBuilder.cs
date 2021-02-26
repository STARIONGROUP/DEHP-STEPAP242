
namespace DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder
{
    using System.Collections.Generic;
    
    using DEHPSTEPAP242.ViewModel.Rows;

    using STEP3DAdapter;

    /// <summary>
    /// Self-referential data source content.
    /// 
    /// Using the following service columns:
    /// - Key Field --> Step3DPartTreeNode.ID
    /// - Parent Field --> Step3DPartTreeNode.ParentID
    /// </summary>
    public class HighLevelRepresentationBuilder : IHighLevelRepresentationBuilder
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
        public List<Step3DRowViewModel> CreateHLR(STEP3DFile step3d)
        {
            if (step3d is null)
            {
                InitializeAuxiliaryData(new STEP3D_Part[0], new STEP3D_PartRelation[0]);
            }
            else
            {
                InitializeAuxiliaryData(step3d.Parts, step3d.Relations);
            }

            var entries = new List<Step3DRowViewModel>();
            int nextID = 1;

            foreach (var p in this.parts)
            {
                if (IsIsolatedPart(p))
                {
                    // Orphan parts are added to the maint Root
                    var node = new Step3DRowViewModel(p, null) { ID = nextID++ };
                    entries.Add(node);

                    // Process part's childs
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

            foreach (var p in this.parts)
            {
                idToPartMap.Add(p.stepId, p);
            }

            foreach (var r in this.relations)
            {
                idToRelationMap.Add(r.stepId, r);

                relatingParts.Add(r.relating_id);
                relatedParts.Add(r.related_id);
            }
        }

        /// <summary>
        /// Verify if a Part does not belong to any other Part
        /// </summary>
        /// <param name="part">A <see cref="STEP3D_Part"/></param>
        /// <returns>True is not used as related part</returns>
        private bool IsIsolatedPart(STEP3D_Part part)
        {
            return !relatedParts.Contains(part.stepId);
        }

        /// <summary>
        /// Gets Children of a Part.
        /// </summary>
        /// <param name="parentId">StepId of a relating Part (parent)</param>
        /// <returns>List of child Part and PartRelation generating the instance</returns>
        private List<(STEP3D_Part, STEP3D_PartRelation)> FindChildren(int parentId)
        {
            if (!partChildren.ContainsKey(parentId))
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
        /// Finds the <see cref="STEP3D_Part"/> object from its StepId.
        /// </summary>
        /// <param name="partId">Step File Id</param>
        /// <returns>The <see cref="STEP3D_Part"/> with requested Id</returns>
        private STEP3D_Part FindPart(int partId)
        {
            return idToPartMap[partId];
        }

        /// <summary>
        /// Adds children of a tree node.
        /// </summary>
        /// <param name="entries">Tree container to fill</param>
        /// <param name="parent">Parent row node</param>
        /// <param name="nextID">Global tree ID for next creation operation</param>
        private void AddSubTree(ICollection<Step3DRowViewModel> entries, Step3DRowViewModel parent, ref int nextID)
        {
            var children = FindChildren(parent.StepId);

            foreach (var cr in children)
            {
                var child = cr.Item1;
                var relation = cr.Item2;

                var node = new Step3DRowViewModel(child, relation)
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
