
namespace DEHPSTEPAP242.Services.FileStoreService
{
    using System.IO;
    using System.Reflection;

    using CDP4Common.EngineeringModelData;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPAP242.Settings;


    /// <summary>
    /// Helper service to cache files from the Hub data source.
    /// 
    /// Provides a mechanism to store a <see cref="FileRevision"/> using a unique name,
    /// keeping track of what was donwloaded to reduce the traffic with the server.
    /// </summary>
    public class FileStoreService : IFileStoreService
    {
        /// <summary>
        /// Default storage directory name
        /// </summary>
        private const string storageDefaultName = "HubFileStorage";

        /// <summary>
        /// Full path to the storage directory
        /// </summary>
        private string StorageDirectoryPath;

        /// <summary>
        /// The <see cref="IUserPreferenceService{AppSettings}"/> instance
        /// </summary>
        private readonly IUserPreferenceService<AppSettings> userPreferenceService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userPreferenceService"></param>
        public FileStoreService(IUserPreferenceService<AppSettings> userPreferenceService)
        {
            this.userPreferenceService = userPreferenceService;

            this.InitializeStorage();
        }

        /// <summary>
        /// Adds a file content for a specific revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <param name="fileContent"></param>
        public void Add(FileRevision fileRevision, byte[] fileContent)
        {
            var destinationPath = this.GetPath(fileRevision);
            System.IO.File.WriteAllBytes(destinationPath, fileContent);
        }

        /// <summary>
        /// Adds a new writable file stream for a specific revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>A <see cref="System.IO.FileStream"/> where perform the write operations</returns>
        public FileStream AddFileStream(FileRevision fileRevision)
        {
            var destinationPath = this.GetPath(fileRevision);
            return new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);
        }

        /// <summary>
        /// Removes existing content in the <see cref="StorageDirectoryPath"/>
        /// </summary>
        public void Clean()
        {
            DirectoryInfo sdi = new DirectoryInfo(this.StorageDirectoryPath);

            foreach (FileInfo file in sdi.EnumerateFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in sdi.EnumerateDirectories())
            {
                dir.Delete(true);
            }
        }

        /// <summary>
        /// Checks if a file for this revision already in the cache area.
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>True if a file exists</returns>
        public bool Exists(FileRevision fileRevision)
        {
            var destinationPath = this.GetPath(fileRevision);
            return System.IO.File.Exists(destinationPath);
        }

        /// <summary>
        /// Gets the corresponding file name in the <see cref="StorageDirectoryPath"/>
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>The file name with pattern [name]_rev[RevisionNumber][extension]</returns>
        public string GetName(FileRevision fileRevision)
        {
            var fileName = Path.GetFileNameWithoutExtension(fileRevision.Path);
            var extension = Path.GetExtension(fileRevision.Path);

            var storageName = $"{fileName}_rev{fileRevision.RevisionNumber}{extension}";
            
            return storageName;
        }

        /// <summary>
        /// Gets the corresponding file path in the <see cref="StorageDirectoryPath"/>
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>Full file path with pattern [StorageDirectoryPath]\\[name]_rev[RevisionNumber][extension]</returns>
        public string GetPath(FileRevision fileRevision)
        {
            var storageName = this.GetName(fileRevision);
            var destinationPath = Path.Combine(StorageDirectoryPath, storageName);

            return destinationPath;
        }

        /// <summary>
        /// Initializes the <see cref="StorageDirectoryPath"/>
        /// </summary>
        public void InitializeStorage()
        {
            string appExecutePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location);

            this.userPreferenceService.Read();
            string dirName = this.userPreferenceService.UserPreferenceSettings.FileStoreDirectoryName;
            bool doClean = this.userPreferenceService.UserPreferenceSettings.FileStoreCleanOnInit;

            if (string.IsNullOrEmpty(dirName))
            {
                this.StorageDirectoryPath = Path.Combine(appExecutePath, storageDefaultName);
            }
            else
            {
                this.StorageDirectoryPath = Path.Combine(appExecutePath, dirName);
            }

            this.CheckStorageDirectory();

            if (doClean)
            {
                this.Clean();
            }
        }

        /// <summary>
        /// Checks for the existence of the <see cref="StorageDirectoryPath"/>
        /// </summary>
        private void CheckStorageDirectory()
        {
            if (!Directory.Exists(this.StorageDirectoryPath))
            {
                Directory.CreateDirectory(this.StorageDirectoryPath);
            }
        }
    }
}
