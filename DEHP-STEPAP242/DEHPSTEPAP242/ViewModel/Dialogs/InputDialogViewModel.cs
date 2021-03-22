
namespace DEHPSTEPAP242.ViewModel.Dialogs
{
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;

    /// <summary>
    /// The view-model for the Login that allows users to open a STEP-AP242 file.
    /// </summary>
    class InputDialogViewModel : IInputDialogViewModel
    {
        /// <summary>
        /// Gets the title of the window
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the label for the information asked to the user
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Gets or sets the answer value
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets the message for a null value
        /// </summary>
        public string NullText { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title">The title of the window</param>
        /// <param name="label">The label of the requested information</param>
        /// <param name="text">The initial value of the requested information</param>
        /// <param name="nulltext">The message in case <paramref name="text"/> is empty</param>
        public InputDialogViewModel(string title, string label, string text, string nulltext = null)
        {
            this.Title = string.IsNullOrEmpty(title) ? "Information requested" : title;
            this.Label = string.IsNullOrEmpty(label) ? "Your value:" : label;
            this.Text = text;
            this.NullText = nulltext;
        }
    }
}
