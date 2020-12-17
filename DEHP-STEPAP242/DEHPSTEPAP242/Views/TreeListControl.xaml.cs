// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TreeListControl.xaml.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2020 RHEA System S.A.
//
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
//
//    This file is part of DEHPSTEPAP242
//
//    The DEHPSTEPAP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
//
//    The DEHPSTEPAP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPSTEPAP242.Views
{
    using System.Collections.Generic;
	using System.Windows.Controls;
    using DevExpress.Mvvm;
	using STEP3DAdapter;

	/// <summary>
	/// Interaction logic for TreeListControl.xaml
	/// </summary>
	public partial class TreeListControl : UserControl
    {
        /// <summary>
        /// Initializes a new <see cref="TreeListControl"/>
        /// </summary>
        public TreeListControl()
        {
            this.InitializeComponent();

            DataContext = new DemoTreeViewModel();
            treeListView.ExpandAllNodes();
        }
    }

    public class Step3DPartTreeNode : STEP3D_Part
    {
        /// <summary>
        /// Auxiliary index for tree control.
        /// </summary>
        public int ID { get => id; }

        /// <summary>
        /// Auxiliary parent index for tree control.
        /// </summary>
        public int ParentID { get; set; }

		#region wrapping structure as properties

		public string Name { get => name;}
        public string Type { get => type;}
        public string RepresentationType { get => representation_type; }
		
        #endregion

		/// <summary>
		/// Compose a reduced description of the ProductDefinition.
		/// </summary>
		public string Description
		{
            get => $"{type}#{id} '{name}'";
		}
    }

    public static class MockStep3DTree
    {
        public static List<Step3DPartTreeNode> GetTree()
        {
            List<Step3DPartTreeNode> staff = new List<Step3DPartTreeNode>();

            staff.Add(new Step3DPartTreeNode() {                 id = 5,   type = "PD", name = "Part", representation_type = "Shape_Representation" });
            staff.Add(new Step3DPartTreeNode() { ParentID = 5,   id = 367, type = "PD", name = "Caja", representation_type = "Advanced_Brep_Shape_Representation" });
            staff.Add(new Step3DPartTreeNode() { ParentID = 5,   id = 380, type = "PD", name = "SubPart", representation_type = "Shape_Representation" });
            staff.Add(new Step3DPartTreeNode() { ParentID = 380, id = 737, type = "PD", name = "Cube", representation_type = "Advanced_Brep_Shape_Representation" });
            staff.Add(new Step3DPartTreeNode() { ParentID = 380, id = 854, type = "PD", name = "Cylinder", representation_type = "Advanced_Brep_Shape_Representation" });

            // ID from id cannot be duplicated. Multiple usage of a PD must be modeled in a different way:
            // ID and ParentID can be sequential numbers
            //staff.Add(new Step3DPartTreeNode() { ParentID = 380, id = 854, type = "PD", name = "Cylinder", representation_type = "Advanced_Brep_Shape_Representation" });
            //staff.Add(new Step3DPartTreeNode() {                 id = 123, type = "X", name = "Extra", representation_type = "None" });

            return staff;
        }
    }

    public class DemoTreeViewModel : ViewModelBase
    {
        public DemoTreeViewModel()
        {
            Step3DHLR = MockStep3DTree.GetTree();
        }
        public List<Step3DPartTreeNode> Step3DHLR { get; private set; }
    }
}
