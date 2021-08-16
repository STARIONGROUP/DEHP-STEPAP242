// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstCompareStepFileVieModel.cs" company="Open Engineering S.A.">
//     Copyright (c) 2021 Open Engineering S.A.
//
//     Author: Ivan Fontaine
//
//     Part of the code was based on the work performed by RHEA as result of the collaboration in
//     the context of "Digital Engineering Hub Pathfinder" by Sam Gerené, Alex Vorobiev, Alexander
//     van Delft and Nathanael Smiechowski.
//
//     This file is part of DEHP STEP-AP242 (STEP 3D CAD) adapter project.
//
//     The DEHP STEP-AP242 is free software; you can redistribute it and/or modify it under the
//     terms of the GNU Lesser General Public License as published by the Free Software Foundation;
//     either version 3 of the License, or (at your option) any later version.
//
//     The DEHP STEP-AP242 is distributed in the hope that it will be useful, but WITHOUT ANY
//     WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
//     PURPOSE. See the GNU Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public License along with this
//     program; if not, write to the Free Software Foundation, Inc., 51 Franklin Street, Fifth
//     Floor, Boston, MA 02110-1301, USA.
// </copyright>
//--------------------------------------------------------------------------------------------------------------------
using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;
using DEHPSTEPAP242.Dialog.Interfaces;
using DEHPSTEPAP242.DstController;
using DEHPSTEPAP242.ViewModel;
using DEHPSTEPAP242.ViewModel.Rows;
using NLog;
using ReactiveUI;
using STEP3DAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static DEHPSTEPAP242.ViewModel.Rows.Step3DDiffRowViewModel;

namespace DEHPSTEPAP242.Dialogs
{
    public class DstCompareStepFilesViewModel : ReactiveObject, IDstCompareStepFilesViewModel
    {
        /** <summary>
         * A list used to store the nodes from both files. Used to initialize the fullNodeLookUp
         * </summary>
         */

        private readonly List<Step3DDiffRowViewModel> fullNodeList = new();
        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;



        /// <summary>
        /// A currentlogger instance.
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /**<summary>
         * Used to store the first file Header viewmodel
         * </summary>
         **/

        public DstStep3DFileHeaderViewModel FirstFileHeader { get; set; }
        /**<summary>
         *Used to store the secod file Header viewmodel
         * </summary>
         **/

        public DstStep3DFileHeaderViewModel SecondFileHeader { get; set; }
        /**<summary>
         * Used to store the first STEP3DFile
         * </summary>
         **/

        private STEP3DFile FirstFile { get; set; }
        /**<summary>
         * Used to store the second STEP3DFIle
         * </summary>
         **/
        private STEP3DFile SecondFile { get; set; }
        /**<summary>
         *The  dstcontroller is required by the HeaderviewModel.
         * </summary>
         **/
        private readonly IDstController dstController;

        /* *<summary>
         * Use to the the HLR data for the first file
         * </summary>
         * */
        private List<Step3DRowData> FirstStepData;

        /* *<summary>
     * Use to the the HLR data for the second file
     * </summary>
     * */
        private List<Step3DRowData> SecondStepData;

        private List<Step3DDiffRowViewModel> step3DHLR = new();

        /// <summary>
        /// Gets or sets the Step3D High Level Representation structure.
        /// </summary>

        public List<Step3DDiffRowViewModel> Step3DHLR
        {
            get => this.step3DHLR;
            private set => this.RaiseAndSetIfChanged(ref this.step3DHLR, value);
        }

        #region Private tree management methods
        /**<summary>
         * Retrieve the children of a node (using the list in Step3DHLR) filtering out the node that are not PartOf the given kind.
         * </summary>
         */

        private List<Step3DDiffRowViewModel> GetVmChildren(int parentID, PartOfKind kind)
        {
            List<Step3DDiffRowViewModel> returnList = new();
            foreach (var node in Step3DHLR)
            {
                if (node.ParentID == parentID && node.PartOf == kind)
                {
                    returnList.Add(node);
                }
            }
            return returnList;
        }

        /**<summary>
        * This method is use to change the "PartOf" member of a complete subtree that has been relocated.
        </summary>

        */

        private void TagSubTree(Step3DDiffRowViewModel root, PartOfKind kind)
        {
            int parentID = root.ID;
            root.PartOf = kind;
            List<Step3DDiffRowViewModel> children = GetVmChildren(parentID, PartOfKind.SECONDTORELOCATE);
            foreach (var child in children)
            {
                TagSubTree(child, kind);
            }
        }

        /**<summary>
         * Connect the newnode as a chid of rootnote then change all the new subtree as belonging to the Second file.
         * </summary>
         */

        private void AddNode(Step3DDiffRowViewModel rootnode, Step3DDiffRowViewModel newnode)
        {
            newnode.ParentID = rootnode.ID; // we just change the parent ID
            TagSubTree(newnode, PartOfKind.SECOND);
        }

        private bool RelocateNode(Step3DDiffRowViewModel node)
        {
            string parentSignature = node.Signature.Substring(0, node.Signature.LastIndexOf('/'));

            Step3DDiffRowViewModel targetNode = null;
            foreach (var treenode in Step3DHLR)
            {
                if (treenode.Signature == parentSignature && treenode.PartOf == PartOfKind.BOTH)
                {
                    logger.Info("Step Diff: relocating the branch {0} from the second file to {1}", node.Signature, treenode.Signature);
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
         * This method will be called repeatedly until there no nodes tagged with the type SECONDTORELOCATE
         * </summary>
         */

        private bool TryToRelocateShortestPath()
        {
            Step3DDiffRowViewModel shortest = null;
            int min = Int32.MaxValue;
            foreach (var node in step3DHLR)
            {
                // Nodes that belongs only to the first file are tagged as being part of the first
                // file we need only to relocate nodes from the second file.
                if (node.PartOf == PartOfKind.SECONDTORELOCATE)
                {
                    int pathLenght = node.Signature.Split('/').Count();
                    if (pathLenght < min)
                    {
                        shortest = node;
                        min = pathLenght;
                        logger.Info("Step Diff: relocating the branch {0} from the second file", node.Signature);
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
         * Main method the build the diff in itself.
         * The algorithm is based on the nodes signature wich is a string that contains the path of the nodes.
         * This current version is based on unique signature for each file. All the nodes names are unique.
         * If the files contain a lot of duplicate names with some big structures modification this could trigger the detection of
         * more differences that there actually is.
         *
         * </summary>
         */

        private bool BuildDiff()
        {
         ILookup<string, Step3DDiffRowViewModel> fullNodeLookup= fullNodeList.ToLookup(x => (x).Signature, x => x);
            // first we consider that all the nodes that have the same signature are equivalent.
                        
            bool noCommonRoot = false;
            bool onlyBoth = true;
            foreach (var values in fullNodeLookup)
            {
                if (values.AsEnumerable().Count() > 1)
                {
                    values.AsEnumerable().First().PartOf = PartOfKind.BOTH;
                    Step3DHLR.Add(values.AsEnumerable().First());
                }
                else
                {
                    if (values.AsEnumerable().First().Signature.Split('/').Count() == 1)
                    {
                        noCommonRoot = true; 
                        
                    }
                    Step3DHLR.Add(values.AsEnumerable().First());
                }
            }

            if (noCommonRoot)
            {
                if (Application.ResourceAssembly != null)
                {
                    Application.Current.Dispatcher.Invoke(() => statusBar.Append("Both step files looks completely different."));
                }
                logger.Info("Step Diff: the two files have no common root node.");
            }
            else
            {
                foreach (var node in step3DHLR)
                {
                    if (node.PartOf == PartOfKind.SECOND)
                    {
                        onlyBoth = false;
                        node.PartOf = PartOfKind.SECONDTORELOCATE;
                    }
                }
                if (onlyBoth  )                  
                {
                    if( Application.ResourceAssembly != null){

                        Application.Current.Dispatcher.Invoke(() => statusBar.Append("Both step files looks the same.")); 
                    }
                    logger.Info("Step Diff: the two files are the same.");
                    
                }
                while (!onlyBoth && TryToRelocateShortestPath()) ;
            }
            // we check if we have some duplicate keys. If we have duplicate we should no try to
            // display the dialog box as it will crash. We normally have no duplicates but this is a
            // security check.
            bool anyDuplicate = step3DHLR.GroupBy(x => x.ID).Any(g => g.Count() > 1);
            return anyDuplicate;
        }
        #endregion Private tree management methods
        #region consructor
        /**<summary>
         * The constructor.
         * </summary>
         */
        public DstCompareStepFilesViewModel(IDstController dstController, IStatusBarControlViewModel statusBarControlViewModel)
        {                        
            this.dstController = dstController;
            this.statusBar = statusBarControlViewModel;
        }
        #endregion constructor

        /** <summary>
         * Sets the path of the files to compare then read them and update the header viewmodel.
         * </summary>
         */
        #region Public methods
        public bool SetFiles(string path1, string path2)
        {
            FirstFile = new STEP3DFile(path1);
            SecondFile = new STEP3DFile(path2);
            if (FirstFile.HasFailed || SecondFile.HasFailed)
            {
                return false;
            }

            FirstFileHeader = new DstStep3DFileHeaderViewModel(dstController);
            SecondFileHeader = new DstStep3DFileHeaderViewModel(dstController);
            FirstFileHeader.File = FirstFile;
            SecondFileHeader.File = SecondFile;

            FirstFileHeader.UpdateHeader();
            SecondFileHeader.UpdateHeader();
            var hlr1 = new HighLevelRepresentationBuilder();
            var hlr2 = new HighLevelRepresentationBuilder();
            var firstHlrData = hlr1.CreateHLR(FirstFile, 1);
            var secondHlrData = hlr2.CreateHLR(SecondFile, firstHlrData.Count + 2);
            SetHlrData(firstHlrData, secondHlrData);
            return true;
        }

        /** <summary>
         * Set the HLR data for both files
         * </summary>
         */

        public void SetHlrData(List<Step3DRowData> first, List<Step3DRowData> second)
        {
            this.FirstStepData = first;
            this.SecondStepData = second;
        }

        /** <summary>
         * Do the file comparison in itself.
         * </summary>
         */

        public bool Process()
        {
            Step3DHLR?.Clear();
           
            fullNodeList?.Clear();

           

            foreach (var dataNode in FirstStepData)
            {
                fullNodeList.Add(new Step3DDiffRowViewModel(dataNode, PartOfKind.FIRST));
            }
            foreach (var dataNode in SecondStepData)
            {
                fullNodeList.Add(new Step3DDiffRowViewModel(dataNode, PartOfKind.SECOND));
            }
            return !BuildDiff();// we have duplicates that's bad.
        }
        #endregion Public Methods
    }
}