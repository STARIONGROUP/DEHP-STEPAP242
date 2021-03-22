
namespace DEHPSTEPAP242.ViewModel.Dialogs.Interfaces
{
    /// <summary>
    /// Interface definiton for <see cref="InputDialogViewModel"/>
    /// </summary>
    interface IInputDialogViewModel
    {
        /// <summary>
        /// Gets the title of the window
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the label for the information asked to the user
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Gets or sets the answer value
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the message for a null value
        /// </summary>
        string NullText { get; }
    }
}
