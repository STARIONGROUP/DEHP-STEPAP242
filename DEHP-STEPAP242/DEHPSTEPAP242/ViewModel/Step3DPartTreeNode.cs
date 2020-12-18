
namespace DEHPSTEPAP242.ViewModel
{
	using System;
	using System.Collections.Generic;
	using STEP3DAdapter;

	public class Step3DPartTreeNode
	{
		internal STEP3D_Part part;

		public Step3DPartTreeNode(STEP3D_Part part)
		{
			this.part = part;
		}

#region HLR tree indexes
		/// <summary>
		/// Auxiliary index for tree control.
		/// </summary>
		public int ID { get; set; }

		/// <summary>
		/// Auxiliary parent index for tree control.
		/// </summary>
		public int ParentID { get; set; }
		
#endregion

#region access to Part fields
		// TODO: add information about the Relation

		public string Name { get => part.name; }
		public string Type { get => part.type; }
		public string RepresentationType { get => part.representation_type; }
		public int StepId { get => part.id; }

		/// <summary>
		/// Compose a reduced description of the ProductDefinition.
		/// </summary>
		public string Description
		{
			get => $"{part.type}#{part.id} '{part.name}'";
		}
		#endregion

		public string RelationLabel { get; set; }
	}

	public static class MockStep3DTree
	{
		public static List<Step3DPartTreeNode> GetTree()
		{
			List<Step3DPartTreeNode> staff = new List<Step3DPartTreeNode>();

			staff.Add(new Step3DPartTreeNode(new STEP3D_Part { id = 5, type = "PD", name = "Part", representation_type = "Shape_Representation" }) { ID = 5 });
			staff.Add(new Step3DPartTreeNode(new STEP3D_Part { id = 367, type = "PD", name = "Caja", representation_type = "Advanced_Brep_Shape_Representation" }) { ID = 367, ParentID = 5 });
			staff.Add(new Step3DPartTreeNode(new STEP3D_Part { id = 380, type = "PD", name = "SubPart", representation_type = "Shape_Representation" }) { ID = 380, ParentID = 5 } );
			staff.Add(new Step3DPartTreeNode(new STEP3D_Part { id = 737, type = "PD", name = "Cube", representation_type = "Advanced_Brep_Shape_Representation" }) { ID = 737, ParentID = 380 });
			staff.Add(new Step3DPartTreeNode(new STEP3D_Part { id = 854, type = "PD", name = "Cylinder", representation_type = "Advanced_Brep_Shape_Representation" }) { ID = 854, ParentID = 380 });

			// ID from id cannot be duplicated. Multiple usage of a PD must be modeled in a different way:
			// ID and ParentID can be sequential numbers
			//staff.Add(new Step3DPartTreeNode() { ParentID = 380, id = 854, type = "PD", name = "Cylinder", representation_type = "Advanced_Brep_Shape_Representation" });
			//staff.Add(new Step3DPartTreeNode() {                 id = 123, type = "X", name = "Extra", representation_type = "None" });

			return staff;
		}
	}
}
