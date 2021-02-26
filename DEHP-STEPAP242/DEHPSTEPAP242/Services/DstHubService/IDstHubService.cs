
namespace DEHPSTEPAP242.Services.DstHubService
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    public interface IDstHubService
    {
        /// <summary>
        /// Checks that all DST required data are in the Hub.
        /// 
        /// Creates any missing data:
        /// - FileTypes
        /// - ParameterTypes
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        Task CheckHubDependencies();

        /// <summary>
        /// First compatible DST <see cref="FileType"/> of a <see cref="FileRevision"/>
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/></param>
        /// <returns>First compatible FileType or null if not found</returns>
        FileType FirstSTEPFileType(FileRevision fileRevision);

        /// <summary>
        /// Finds all the revisions for DST files
        /// </summary>
        /// <returns></returns>
        List<FileRevision> GetFileRevisions();

        /// <summary>
        /// Checks if it is a STEP file type
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>True if is a STEP file</returns>
        bool IsSTEPFileType(FileRevision fileRevision);

        /// <summary>
        /// Checks if a parameter is compatible with STEP 3D mapping
        /// </summary>
        /// <param name="param">The <see cref="ParameterType"/> to check</param>
        /// <returns>True if it is a candidate for the mapping</returns>
        bool IsSTEPParameterType(ParameterType param);

        /// <summary>
        /// Gets the step geometric parameter where to store a STEP-AP242 part information
        /// </summary>
        /// <returns>A <see cref="ParameterType"/></returns>
        public ParameterType FindSTEPParameterType();

        /// <summary>
        /// Gets the <see cref="ParameterTypeComponent"/> corresponding to the source file reference
        /// </summary>
        /// <param name="param">The <see cref="ParameterType"/> to check</param>
        /// <returns>A <see cref="ParameterTypeComponent"/> or null if does not contain the component</returns>
        public ParameterTypeComponent FindSourceParameterType(ParameterType param);

        /// <summary>
        /// Gets the <see cref="ReferenceDataLibrary"/> where to add DST content
        /// </summary>
        /// <returns>A <see cref="ReferenceDataLibrary"/></returns>
        ReferenceDataLibrary GetReferenceDataLibrary();

        /// <summary>
        /// Finds the DST <see cref="CDP4Common.EngineeringModelData.File"/> in the Hub
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.File"/> or null if does not exist</returns>
        File FindFile(string filePath);

        /// <summary>
        /// Finds the <see cref="CDP4Common.EngineeringModelData.FileRevision"/> from string <see cref="System.Guid"/>
        /// </summary>
        /// <param name="guid">The string value of an <see cref="System.Guid"/></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.FileRevision"/> or null if does not exist</returns>
        FileRevision FindFileRevision(string guid);
    }
}
