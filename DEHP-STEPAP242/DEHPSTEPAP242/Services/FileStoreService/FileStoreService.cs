
namespace DEHPSTEPAP242.Services.FileStoreService
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Reflection;

    using CDP4Common.EngineeringModelData;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPAP242.Settings;

    public class FileStoreService : IFileStoreService
    {
        private const string storageDefaultName = "HubFileStorage";

        /// <summary>
        /// Full path to the storage directory
        /// </summary>
        public string StorageDirectoryPath;

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

            InitializeStorage();
        }

        /// <summary>
        /// Adds a file content for a specific revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <param name="fileContent"></param>
        public void Add(FileRevision fileRevision, byte[] fileContent)
        {
            var destinationPath = GetPath(fileRevision);
            System.IO.File.WriteAllBytes(destinationPath, fileContent);
        }

        /// <summary>
        /// Adds a new writable file stream for a specific revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>A <see cref="System.IO.FileStream"/> where perform the write operations</returns>
        public FileStream AddFileStream(FileRevision fileRevision)
        {
            return new System.IO.FileStream(GetPath(fileRevision), System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);
        }

        /// <summary>
        /// Removes existing content in the <see cref="StorageDirectoryPath"/>
        /// </summary>
        public void Clean()
        {
            DirectoryInfo sdi = new DirectoryInfo(StorageDirectoryPath);

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
            var path = GetPath(fileRevision);
            return System.IO.File.Exists(path);
        }

        /// <summary>
        /// Gets the corresponding file in the <see cref="StorageDirectoryPath"/>
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>Full file path with pattern [StorageDirectoryPath]\\[name]_rev[RevisionNumber][extension]</returns>
        public string GetPath(FileRevision fileRevision)
        {
            var fileName = Path.GetFileNameWithoutExtension(fileRevision.Path);
            var extension = Path.GetExtension(fileRevision.Path);

            var storageName = $"{fileName}_rev{fileRevision.RevisionNumber}{extension}";
            var path = Path.Combine(StorageDirectoryPath, storageName);

            return path;
        }

        /// <summary>
        /// Initializes the <see cref="StorageDirectoryPath"/>
        /// </summary>
        public void InitializeStorage()
        {
            string appExecutePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location);

            userPreferenceService.Read();
            string dirName = userPreferenceService.UserPreferenceSettings.FileStoreDirectoryName;
            bool doClean = userPreferenceService.UserPreferenceSettings.FileStoreCleanOnInit;

            if (string.IsNullOrEmpty(dirName))
            {
                StorageDirectoryPath = Path.Combine(appExecutePath, storageDefaultName);
            }
            else
            {
                StorageDirectoryPath = Path.Combine(appExecutePath, dirName);
            }

            CheckStorageDirectory();

            if (doClean) Clean();

            // /* Configuration from the App.config */
            //var storageSettings = ConfigurationManager.GetSection("FileStoreSettings") as NameValueCollection;
            //
            //string dirName = storageSettings["DirectoryName"];
            //bool doClean = storageSettings["CleanOnInit"] == "true";
            //
            //if (doClean) Clean();
        }

        /// <summary>
        /// Checks for the existence of the <see cref="StorageDirectoryPath"/>
        /// </summary>
        private void CheckStorageDirectory()
        {
            if (!Directory.Exists(StorageDirectoryPath))
            {
                Directory.CreateDirectory(StorageDirectoryPath);
            }
        }
    }
}
