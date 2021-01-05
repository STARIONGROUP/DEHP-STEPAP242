#pragma once

using namespace System;

#include "step3d_wrapper.h"
#include "Tools/Tools.h"


namespace STEP3DAdapter
{
    // NOTE: C++ comment style looks more readable for structs.
    // Also I noticed that this documentation is not exported to
    // the C# interface (automatic metadata exportation).
    //
    // For the moment I will document struct member in same line,
    // taking the information from step3d_wrapper.h file.


    /// <summary>
    /// Managed version of <c>File_Description_Wrapper</c> struct.
    /// </summary>
    public ref struct STEP3D_File_Description
    {
        String^ description;
        String^ implementation_level;
    };

    /// <summary>
    /// Managed version of <c>File_Name_Wrapper</c> struct.
    /// </summary>
    public ref struct STEP3D_File_Name
    {
        String^ name;
        String^ time_stamp;
        String^ author;
        String^ organization;
        String^ preprocessor_version;
        String^ originating_system;
        String^ authorisation;
    };

    /// <summary>
    /// Managed version of <c>Step3D_HeaderInfo_Wrapper</c> struct.
    /// </summary>
    public ref struct STEP3D_HeaderInfo
    {
        STEP3D_File_Description^ file_description;  //!< File Description
        STEP3D_File_Name^ file_name;                //!< File Name
        String^ file_schema;                        //!< File Schema identifier
    };
    
    /// <summary>
    /// Managed version of <c>Part_Wrapper</c> struct.
    /// </summary>
    public ref struct STEP3D_Part
    {
        // From product definition
        int stepId;                           //!< PD.stepId (in the STEP file)
        String^ type;                         //!< ENTITY TYPE (STEP class name) = PD
        String^ name;                         //!< PD.PDF.P.name

        // From used_representation (geometry)
        String^ representation_type;          //!< ENTITY TYPE (STEP class name) - only SR and ABSP are managed
    };

    /// <summary>
    /// Managed version of <c>Relation_Wrapper</c> struct.
    /// </summary>
    public ref struct STEP3D_PartRelation
    {
        int stepId;            //!< NAUO.stepId (in the STEP file)
        String^ type;          //!< ENTITY TYPE (class name) = NAUO
        String^ id;            //!< NAUO.id
        String^ name;          //!< NAUO.name

        int relating_id;   //!< PD.id of the parent (in the STEP file)
        int related_id;    //!< PD.id of the child (in the STEP file)

//#define WITH_RELATION_PART_REFERENCES
#ifdef WITH_RELATION_PART_REFERENCES
        // This pointers are a code test to confirm that C# references
        // work as expected: these relations points to the same part.
        STEP3D_Part^ relating_part;
        STEP3D_Part^ related_part;
#endif
    };

    /// <summary>
    /// The <see cref="STEP3DFile"/> class is a C++/.NET wrapper which provides
    /// access to the content of a STEP3D (Application Protocol 242) provided
    /// by the step3d_wrapper.dll dynamic library.
    /// 
    /// This is a read-only class, there are not implementations in the current
    /// step3d_wrapper.dll library which permit change in some way the target file.
    /// </summary>
    public ref class STEP3DFile
    {
    public:

        /// <summary>
        /// Initializes a new instance of the <see cref="STEP3DFile"/> class
        /// 
        /// Before retrieve the information you must verify if the content 
        /// was correctly loaded.
        /// </summary>
        /// <param name="fileName">full path to STEP3D file (.step|.stp)</param>
        STEP3DFile(String^ fileName);

        ~STEP3DFile();

        !STEP3DFile();

        /// <summary>
        /// Asserts whether the step3d_wrapper.dll has errors in the operation.
        /// </summary>
        property bool HasFailed
        {
            bool get()
            {
                return m_wrapper->hasFailed();
            }
        }

        /// <summary>
        /// Gets last error message.
        /// </summary>
        property String^ ErrorMessage
        {
            String^ get()
            {
                return Tools::toString(m_wrapper->getErrorMessage());
            }
        }

        /// <summary>
        /// Gets the working file name.
        /// </summary>
        property String^ FileName
        {
            String^ get()
            {
                return Tools::toString(m_wrapper->getFilename());
            }
        }

        /// <summary>
        /// Gets information from the HEADER section of the STEP file.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="STEP3D_HeaderInfo"/> struct.
        /// </returns>
        property STEP3D_HeaderInfo^ HeaderInfo
        { 
            STEP3D_HeaderInfo^ get() 
            {
                return m_headerInfo;
            }
        }

        /// <summary>
        /// Returns the list of geometrical parts detected.
        /// </summary>
        /// <returns>
        /// An array of <see cref="STEP3D_Part"/> struct.
        /// </returns>
        property array<STEP3D_Part^>^ Parts
        {
            array<STEP3D_Part^>^ get()
            {
                return m_parts;
            }
        }

        /// <summary>
        /// Returns the list of parent/child relation between parts.
        /// </summary>
        /// <returns>
        /// An array of <see cref="STEP3D_PartRelation"/> struct.
        /// </returns>
        property array<STEP3D_PartRelation^>^ Relations
        {
            array<STEP3D_PartRelation^>^ get()
            {
                return m_relations;
            }
        }

        /// <summary>
        /// The step3d_wrapper.dll contains information about the version
        /// containing also the build datetime.
        /// </summary>
        /// <returns>
        /// STEPcode string version.
        /// </returns>
        static property String^ STEPcodeVersion
        {
            String^ get()
            {
                return Tools::toString(getStepcodeVersion());
            }
        }

    private:
        /// <summary>
        /// Unmanaged reference to <see cref="ISTEP3D_Wrapper"/> obtained from step3d_wrapper.dll.
        /// 
        /// It is available at any time, instantiated once by the constructor.
        /// </summary>
        IStep3D_Wrapper* m_wrapper;

        /// <summary>
        /// Managed struct of the header's information.
        /// </summary>
        STEP3D_HeaderInfo^ m_headerInfo;

        /// <summary>
        /// Managed array of detected geometrical parts.
        /// </summary>
        array<STEP3D_Part^>^ m_parts;

        /// <summary>
        /// Managed array of detected geometrical parts.
        /// </summary>
        array<STEP3D_PartRelation^>^ m_relations;

        /// <summary>
        /// Initialize instance
        /// </summary>
        void initializeEmpty();

        /// <summary>
        /// Convert from unmanaged to managed data.
        /// </summary>
        void convertHeaderInfo();

        /// <summary>
        /// Convert from unmanaged to managed data.
        /// </summary>
        void convertParts();

        /// <summary>
        /// Convert from unmanaged to managed data.
        /// </summary>
        void convertPartRelations();

        /// <summary>
        /// Creates a managed struct for a Part_Wrapper.
        /// </summary>
        /// <param name="pw">Identified geometrical part in the STEP file</param>
        /// <returns>
        /// An instance of <see cref="STEP3D_Part"/> struct.
        /// </returns>
        STEP3D_Part^ createPart(const Part_Wrapper& pw);

        /// <summary>
        /// Creates the managed structure for a Relation_Wrapper.
        /// </summary>
        /// <param name="pw">Identified parts' relation in the STEP file</param>
        /// <returns>
        /// An instance of <see cref="STEP3D_PartRelation"/> struct.
        /// </returns>
        STEP3D_PartRelation^ createRelationPart(const Relation_Wrapper& rw);
    };
}


