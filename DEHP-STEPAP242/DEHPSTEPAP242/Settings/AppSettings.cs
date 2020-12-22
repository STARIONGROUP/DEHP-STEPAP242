
namespace DEHPSTEPAP242.Settings
{
    using System.Collections.Generic;

    using DEHPCommon.UserPreferenceHandler;

    /// <summary>
    /// Extends the <see cref="UserPreference"/> class and acts as a container for the locally saved user settings
    /// </summary>
    public class AppSettings : UserPreference
    {
        /// <summary>
        /// The list of recently loaded file paths
        /// </summary>
        public List<string> RecentFiles { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettings"/> class
        /// </summary>
        public AppSettings()
        {
            this.RecentFiles = new List<string>();
        }
    }
}
