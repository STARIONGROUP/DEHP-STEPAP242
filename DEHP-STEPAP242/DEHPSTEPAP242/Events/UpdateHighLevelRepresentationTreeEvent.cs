

namespace DEHPSTEPAP242.Events
{
    using DEHPCommon.Events;

    /// <summary>
    /// An event for <see cref="CDP4Dal.CDPMessageBus"/>
    /// </summary>
    public class UpdateHighLevelRepresentationTreeEvent : UpdateTreeBaseEvent
    {
        /// <summary>
        /// Initializes a new <see cref="UpdateHighLevelRepresentationTreeEvent"/>
        /// </summary>
        public UpdateHighLevelRepresentationTreeEvent(bool reset = false) : base(reset)
        {
        }
    }
}