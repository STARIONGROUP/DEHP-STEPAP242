

namespace DEHPSTEPAP242.ViewModel.Interfaces
{
	using System.Collections.Generic;

	using STEP3DAdapter;

	///	<summary>
	/// Interface definition for <see cref="DstObjectBrowserViewModel"/>
	/// </summary>
	public interface IDstObjectBrowserViewModel
	{
		public List<Step3DPartTreeNode> Step3DHLR { get; }

		public void UpdateHLR(STEP3D_Part[] parts, STEP3D_PartRelation[] relations);
	}
}
