namespace DEHPSTEPAP242.Services.FileStoreService
{
	using CDP4Common.EngineeringModelData;

	/// <summary>
	/// Service to store and cache files downloaded from the Hub.
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
		/// Adds the file for a specific revision
		/// </summary>
		/// <param name="fileRevision"></param>
		/// <param name="fileContent"></param>
		void Add(FileRevision fileRevision, byte[] fileContent);

		/// <summary>
		/// Checks if a file revision is already in the cache.
		/// </summary>
		/// <param name="fileRevision"></param>
		/// <returns></returns>
		bool Exists(FileRevision fileRevision);

		/// <summary>
		/// Get the path for a file revision
		/// </summary>
		/// <param name="fileRevision"></param>
		/// <returns>Returns null if does not exists</returns>
		string GetPath(FileRevision fileRevision);
	}
}
