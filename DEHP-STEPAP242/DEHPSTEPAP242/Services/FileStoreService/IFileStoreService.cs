namespace DEHPSTEPAP242.Services.FileStoreService
{
    using CDP4Common.EngineeringModelData;

    /// <summary>
    /// Service to store and cache files downloaded from the <see cref="DomainFileStore"/>.
    /// </summary>
    public interface IFileStoreService
    {
        /// <summary>
        /// Initializes the directory where files from the Hub are stored
        /// </summary>
        void InitializeStorage();

        /// <summary>
        /// Cleans all previous downloaded files
        /// </summary>
        void Clean();

        /// <summary>
        /// Adds a file content for a specific revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <param name="fileContent"></param>
        void Add(FileRevision fileRevision, byte[] fileContent);

        /// <summary>
        /// Adds a new writable file stream for a specific revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>A <see cref="System.IO.FileStream"/> where perform the write operations</returns>
        System.IO.FileStream AddFileStream(FileRevision fileRevision);

        /// <summary>
        /// Checks if a file for this revision already in the cache area.
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>True if a file exists</returns>
        bool Exists(FileRevision fileRevision);

        /// <summary>
        /// Get the path for a file revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>Returns null if does not exists</returns>
        string GetPath(FileRevision fileRevision);
    }
}
