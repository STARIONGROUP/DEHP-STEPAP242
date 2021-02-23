
namespace DEHPSTEPAP242.Settings
{
    using System.Collections.Generic;

    using DEHPCommon.UserPreferenceHandler;

    /// <summary>
    /// Extends the <see cref="UserPreference"/> class and acts as a container
    /// for the locally saved user settings.
    /// </summary>
    public class AppSettings : UserPreference
    {
        /// <summary>
        /// The list of recently loaded file paths
        /// </summary>
        public List<string> RecentFiles { get; set; }

        /// <summary>
        /// The dictionary relating a STEP file name with an <see cref="ExternalIdentifierMap"/> name
        /// </summary>
        public Dictionary<string, string> MappingUsedByFiles { get; set; }

        /// <summary>
        /// Directory name for the local FileStore directory in the <see cref="FileStoreService"/>
        /// </summary>
        public string FileStoreDirectoryName { get; set; }

        /// <summary>
        /// Intructs the <see cref="FileStoreService"/> to automatically clean on service initialization
        /// </summary>
        public bool FileStoreCleanOnInit { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettings"/> class
        /// </summary>
        public AppSettings()
        {
            RecentFiles = new List<string>();
            MappingUsedByFiles = new Dictionary<string, string>();

            FileStoreDirectoryName = "TempHubFiles";
            FileStoreCleanOnInit = false;
        }
    }
}
