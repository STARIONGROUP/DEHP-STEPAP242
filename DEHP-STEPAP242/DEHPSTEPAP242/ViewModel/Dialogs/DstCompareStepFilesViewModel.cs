using DEHPCommon.UserInterfaces.Behaviors;
using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;
using DEHPSTEPAP242.Dialog.Interfaces;
using DEHPSTEPAP242.DstController;
using DEHPSTEPAP242.ViewModel;
using DEHPSTEPAP242.ViewModel.Interfaces;
using DEHPSTEPAP242.ViewModel.Rows;
using DEHPSTEPAP242.Views;
using DEHPSTEPAP242.Views.Dialogs;
using ReactiveUI;
using STEP3DAdapter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static DEHPSTEPAP242.ViewModel.Interfaces.IDstNodeDiffData;
using static DEHPSTEPAP242.ViewModel.Rows.Step3DDiffRowViewModel;

namespace DEHPSTEPAP242.Dialogs
{
     class DstCompareStepFilesViewModel : ReactiveObject, IDstCompareStepFilesViewModel, ICloseWindowViewModel
    {
        // <summary>
        // This inner private class will be use to store information that will be used to build the resulting
        // diff tree
        //</summary?
        private class DstFacadeNode: IDstNodeDiffData
        {
            public int ID { set; get; }
            public int ParentID { set; get; }
            public string Name { get => ""; }
            public string Signature{ get=> "/"; }
            public PartOfKind PartOf { get => PartOfKind.BOTH; set { } }







        }

        public PartOfKind PartOf { get; set; }
        private List<Step3DDiffRowViewModel> rdList = new();

        private ILookup<string, Step3DDiffRowViewModel> rdLookup;

      //  private List<List<Step3DRowData>> MustMerge = new();

        //private List<Step3DDiffRowViewModel> vmList = new();
        private bool loadingFile;

        /// <summary>
        /// Gets or sets the current loading task status.
        /// </summary>
        public bool IsLoadingFile
        {
            get => loadingFile;
            set => this.RaiseAndSetIfChanged(ref this.loadingFile, value);
        }


        private int NewID { get; set; }
        public DstStep3DFileHeaderViewModel FirstFileHeader { get; set; }
        public DstStep3DFileHeaderViewModel SecondFileHeader { get; set; }
        private STEP3DFile FirstFile { get;  set; }
        private STEP3DFile SecondFile { get; set; }
        private List<IDstNodeDiffData> step3Work = new();

        private List<IDstNodeDiffData> step3DHLR;//= new List<Step3DDiffRowViewModel>();

        /// <summary>
        /// Gets or sets the Step3D High Level Representation structure.
        /// </summary>
        ///
       

        public List<IDstNodeDiffData> Step3DHLR
        {
            get => this.step3DHLR;
            private set => this.RaiseAndSetIfChanged(ref this.step3DHLR, value);
        }

        public ICloseWindowBehavior CloseWindowBehavior { get; set; }
        
       

       

        private List<IDstNodeDiffData> GetVmChildren(int parentID, PartOfKind kind)
        {
            List<IDstNodeDiffData> returnList = new();
            foreach (var node in step3Work)
            {
                if (node.ParentID == parentID && node.PartOf == kind)
                {
                    returnList.Add(node);
                }
            }
            return returnList;
        }

        private void TagSubTree(IDstNodeDiffData   root, PartOfKind kind)
        {
            int parentID = root.ID;
            root.ID = NewID++;
            root.PartOf = kind;
            List<IDstNodeDiffData> children = GetVmChildren(parentID, PartOfKind.SECONDTORELOCATE);
            foreach (var child in children)
            {
                child.ParentID = root.ID;
                TagSubTree(child, kind);
            }
        }

        private void AddNode(IDstNodeDiffData rootnode, IDstNodeDiffData newnode)
        {
            newnode.ParentID = rootnode.ID; // we just change the parent ID

            TagSubTree(newnode, PartOfKind.SECOND);
        }

        private bool RelocateNode(IDstNodeDiffData  node)
        {
            // string nodeSignature = node.Signature;
            string parentSignature = node.Signature.Substring(0, node.Signature.LastIndexOf('/'));
            // as the HLR only contains a node
            IDstNodeDiffData targetNode = null;
            foreach (var treenode in step3Work)
            { 
                if (treenode.Signature == parentSignature && treenode.PartOf == PartOfKind.BOTH)
                {
                    targetNode = treenode;
                    break;
                }
            }
            if (targetNode == null)
            {
                return false;// cannot relocate... there is a problem
            }

            AddNode(targetNode, node);
            return true;
        }
        /**<summary>
         * This method try to relocate a nodes whose signature is one of  shortest one among the nodes to relocate.
         * We then reconnect all of its children to the reallocated nodes. (From shortest descending the tree towards the leafs)
         * </summary>
         */
        private bool TryToRelocateShortestPath()
        {
            IDstNodeDiffData shortest = null;
            int min = Int32.MaxValue;
            foreach (var node in step3Work)
            {// In Step3DHLR in case of signature equality we use the node from the first tree
             // Node that belongs only to the first tree are tagged as being part of the first file
             // we need only to relocate nodes from the second file.
                if (node.PartOf == PartOfKind.SECONDTORELOCATE)
                {
                    int pathLenght = node.Signature.Split('/').Count();
                    /* Here we need to relocate the node to a new root. In fact we need to create a new node that will be the new root for both trees.
                     */
                    if (pathLenght == 1)
                    {
                        var newRootData = new DstFacadeNode() { ID = -1 };
                        foreach (var oldrootnode in step3Work)
                        {
                            if(oldrootnode.PartOf==PartOfKind.BOTH && oldrootnode.Signature.Split('/').Count() == 2)
                            {
                                oldrootnode.ParentID = -1;

                            }

                        }



                    }
                    if (pathLenght < min)
                    {
                        shortest = node;
                        min = pathLenght;
                    }
                }
            }

            if (min == Int32.MaxValue)
            {
                return false;// we do not have a node to relocate
            }
            return RelocateNode(shortest);
        }

        /** <summary>
         * To identify the subtree we compute a Hash function for every subtree. When to subtree have the same hash
         * value we consider that they are equal. The "unique name" thing is to avoid to have duplicate name that whould produce the same hash value.
         * This is an euristic algorithm we will be able to process correctly all the differences. But when the file share a similar structure it is going to work.
         * </summary>
         */

                    private void BuildDiff()
        {
            rdLookup = rdList.ToLookup(x => ((IDstNodeDiffData)x).Signature, x => x);
            // first we consider that all the nodes that have the same signature are  equivalent.
            //
            foreach (var values in rdLookup)
            {
                if (values.AsEnumerable().Count() > 1)
                {
                    ((IDstNodeDiffData)values.AsEnumerable().First()).PartOf = PartOfKind.BOTH;

                    step3Work.Add(values.AsEnumerable().First());
                }
                else
                {
                    if (((IDstNodeDiffData)values.AsEnumerable().First()).PartOf == PartOfKind.SECOND)
                    {
                        ((IDstNodeDiffData)values.AsEnumerable().First()).PartOf = PartOfKind.SECONDTORELOCATE;
                    }

                    step3Work.Add(values.AsEnumerable().First());
                }
            };

            // the Stephlr now contains all the node we need.
            // Next step is to try to connect "orphan subtree"
            while (TryToRelocateShortestPath()) ;
        }

       

        private List<Step3DRowData> FirstStepData;

        private List<Step3DRowData> SecondStepData = new();

        public DstCompareStepFilesViewModel(IDstController dstController        )
        {
            // Here we initialize the two header view model used to describe the loaded files.
            FirstFileHeader = new DstStep3DFileHeaderViewModel(dstController);
            SecondFileHeader = new DstStep3DFileHeaderViewModel(dstController);          
        }
         /** <summary>
          * Sets the path of the file to compare then read them and update the header viewmodel.
          * </summary>
          */
        public void SetFiles(string path1, string path2)
        {
            
            FirstFile = new STEP3DFile(path1);
            SecondFile = new STEP3DFile(path2);
            FirstFileHeader.File = FirstFile;
            SecondFileHeader.File = SecondFile;
            FirstFileHeader.UpdateHeader();
            SecondFileHeader.UpdateHeader();


        }
        /** <summary>
         * Do the file comparison in itself.
         * </summary>
         */
        public async Task Process()
        {           
            var hlr1 = new HighLevelRepresentationBuilder();
            var hlr2 = new HighLevelRepresentationBuilder();

            FirstStepData = hlr1.CreateHLR(FirstFile);
            SecondStepData = hlr2.CreateHLR(SecondFile);            

            foreach (var dataNode in FirstStepData)
            {
                rdList.Add(new Step3DDiffRowViewModel(dataNode, PartOfKind.FIRST));
            }
            foreach (var dataNode in SecondStepData)
            {
                rdList.Add(new Step3DDiffRowViewModel(dataNode, PartOfKind.SECOND));
            }            
            BuildDiff();         
            Step3DHLR = step3Work;          
            }
    }



        

}



