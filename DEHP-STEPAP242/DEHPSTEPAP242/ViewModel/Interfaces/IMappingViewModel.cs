
namespace DEHPSTEPAP242.ViewModel.Interfaces
{
    using DEHPSTEPAP242.ViewModel.Rows;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for <see cref="MappingViewModel"/>
    /// </summary>
    public interface IMappingViewModel
    {
        /// <summary>
        /// Gets or sets the collection of <see cref="MappingRows"/>
        /// </summary>
        ReactiveList<MappingRowViewModel> MappingRows { get; set; }
    }
}
