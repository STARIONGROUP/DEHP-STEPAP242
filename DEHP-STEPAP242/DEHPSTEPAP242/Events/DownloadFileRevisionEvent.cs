
namespace DEHPSTEPAP242.Events
{
    /// <summary>
    /// An event for <see cref="CDP4Dal.CDPMessageBus"/>
    /// </summary>
    public class DownloadFileRevisionEvent
    {
        /// <summary>
        /// The target thing id
        /// </summary>
        public string TargetId { get; }

        /// <summary>
        /// Initializes a new <see cref="DownloadFileRevisionEvent"/>
        /// </summary>
        /// <param name="targetId">The target thing id</param>
        /// <param name="shouldHighlight">A Value indicating whether the higlighting of the target should be canceled</param>
        public DownloadFileRevisionEvent(string targetId)
        {
            this.TargetId = targetId;
        }
    }
}
