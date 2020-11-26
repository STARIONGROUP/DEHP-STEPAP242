#pragma once

/**
* Public Interface of the STEP-3D Wrapper library.
* This header files contains the complete interface.
* 
* Reference to AP242: <a href="https://www.cax-if.org/documents/AP242/AP242_mim_lf_1.36.htm"></a>
* 
* @note Linked to Stepcode shared libraries.
*/

#include "step3d_dllinterface.h"


#include <string>
#include <list>


/**
* @brief Stepcode version string
* 
* String containing the last tagged git revision of the Stepcode repository.
* 
* Example: "git commit id: v0.8-241-ga9a7e0af, build timestamp 2020-11-23T19:58:17Z"
* 
* Reference: <a href="https://github.com/stepcode/stepcode"></a>
*/
STEP3D_DLLAPI char* getStepcodeVersion();


/**
* @brief STEP file's description section
* 
* This contains the information form the Header section
* common to all the STEP files (independently of the AP).
*/
struct STEP3D_DLLAPI Step3D_HeaderInfo_Wrapper
{
    struct File_Description_Wrapper
    {
        std::string description;
        std::string implementation_level;
    };

    struct File_Name_Wrapper
    {
        std::string name;
        std::string time_stamp;
        std::string author;
        std::string organization;
        std::string preprocessor_version;
        std::string originating_system;
        std::string authorisation;
    };

    File_Description_Wrapper file_description;  //!< File Description
    File_Name_Wrapper file_name;                //!< File Name
    std::string file_schema;                    //!< File Schema identifier
};

#ifdef test_description_ctype
struct STEP3D_DLLAPI TDescriptionNodeCType
{
    const char* File_Name;
    const char* File_Description; // SdaiFile_description
    const char* File_Description_Id; // SdaiFile_description
    const char* File_Schema;
};
#endif


typedef double CartesianPoint_Wrapper[3];
typedef double Direction_Wrapper[3];

/**
* @brief 3D Placement representation
* 
* Represents the placement (position and orientation) 
* in the 3D space for a geometric part.
* 
* It is also used in the Item_Defined_Transformation STEP entity
* associtated to the Next_Assembly_Usage_Occurrence entity.
*/
struct STEP3D_DLLAPI Axis2_Placement_3d_Wrapper
{
    std::string name;
    CartesianPoint_Wrapper location;
    Direction_Wrapper axis;
    Direction_Wrapper ref_direction;

    Axis2_Placement_3d_Wrapper()
    {
        location[0] = 0;
        location[1] = 0;
        location[2] = 0;

        axis[0] = 0;
        axis[1] = 0;
        axis[2] = 0;

        ref_direction[0] = 0;
        ref_direction[1] = 0;
        ref_direction[2] = 0;
    }
};

/**
* @brief Part representation
* 
* A part, or component, is the CAD entity exported
* in the STEP file as Product.
* 
* It resumes the relevant information which was
* obtanied from all the related STEP entities (note
* that a simple concept is modeled through many
* related entities).
* 
* The information comes from two input sources:
* - definition: Product_Definition_Shape
* - used_representation: Representation
*/
struct STEP3D_DLLAPI Part_Wrapper
{
    // From definition
    int id;                                   //!< PD.id (in the STEP file)
    std::string type;                         //!< ENTITY TYPE (STEP class name) = PD
    std::string name;                         //!< PD.PDF.P.name

    // Others properties could be:
    // - P.description
    // - PDF.description
    // - PDC.life_cycle_stage
    // - PDC.AC.application


    // From used_representation (geometry)
    Axis2_Placement_3d_Wrapper placement;     //!< Geometry placement (local, not absolute) - from the R.items[]
    std::string representation_type;                         //!< ENTITY TYPE (STEP class name)- only SR and ABSP are managed

    // Other Representation properties could be:
    // - R.name (empty in all examples)

    Part_Wrapper() : id(0) {}
};

/**
* @brief Relation (comoposition) representation
* 
* The hierarchy CAD structure is exported
* in the STEP file as relations (or links)
* between two Product definitions.
* 
* It resumes the relevant information which was
* obtanied from all the related STEP entities (note
* that a simple concept is modeled through many
* related entities).
* 
* The information comes from two input sources:
* - relating: Product_Definition (ignore other targets)
* - related: Product_Definition (ignore other targets)
* - transformation : Context_Dependent_Shape_Representation (parent entity to finally arrives to the transformation)
*/
struct STEP3D_DLLAPI Relation_Wrapper
{
    int id;            //!< NAUO.id (in the STEP file)
    std::string type;  //!< ENTITY TYPE (class name) = NAUO
    std::string name;  //!< NAUO.label

    int relating_id;   //!< PD.id of the parent (in the STEP file)
    int related_id;    //!< PD.id of the child (in the STEP file)

    // From transformation:
    // - CDSR.PDS.definition == NAUO (should match the current NAUO entity)
    // - CDSR.SRR.RRWT
    // - RRWT.IDT.transform_item_1 of type Axis2_Placement_3d (ignore others targets)
    // - RRWT.IDT.transform_item_2 of type Axis2_Placement_3d (ignore others targets)
};

enum class WrapperErrorCode
{
    NO_ERROR = 0,
    FILE_NOT_FOUND,
    FILE_READ,
    FILE_PROCESS,
    NOT_IMPLEMENTED,
    UNKNOWN_ERROR = 1000,
};

/**
* @brief Interface to a STEP3D manager
* 
* The STEP3D is the short name for STEP-AP242 file format.
* 
* This interface is wrapper to the complex Stepcode 
* library, which gives a simplified access to the information
* used in the construction of the High Level Representation (HLR).
* 
* The information is exported using Entity's IDs instead pointers
* to Stepcode's objects to keep the header simple.
*/
class STEP3D_DLLAPI IStep3D_Wrapper
{
public:
    /**
    * @brief Load STEP-3D file
    * @param[in] fname full path to .stp|.step file
    * @return true if the file was correctly loaded
    * 
    * This method can be called once.
    * 
    * @note Call createHLR() to collect the useful information.
    */
    virtual bool load(std::string fname) = 0;

    /**
    * @brief Get file name of loaded file
    * @return file name used in the Load() method
    */
    virtual std::string getFilename() = 0;

    /**
    * @brief Parse the file content and extract HLR information
    * 
    * The HLR requires:
    * - Description of the file (general information, HEADER section)
    * - Products (geometrical objects, the DATA section)
    * - Relations (relation between Products, DATA section).
    * 
    * @sa getDescription()
    * @sa getNodes()
    * @sa getRelations()
    */
    virtual bool parseHLRInformation() = 0;

    /**
    * @brief Get description of the file
    */
    virtual Step3D_HeaderInfo_Wrapper getHeaderInfo() = 0;
#ifdef test_description_ctype
    virtual TDescriptionNodeCType getDescriptionCType() = 0;
#endif

    /**
    * @brief Get list HLR tree's nodes
    * 
    * A HLR tree's node represents the information of a
    * geometrical part. The information is collected from
    * different STEP entities.
    */
    virtual std::list<Part_Wrapper> getNodes() = 0;

    /**
    * @brief Get list HLR tree's relations
    * 
    * A HLR tree's relation represents the link between two
    * geometrical parts. The information is collected from
    * different STEP entities. 
    */
    virtual std::list<Relation_Wrapper> getRelations() = 0;

    /**
    * @brief Check if the last action finished with errors
    * 
    * If there are errors:
    * - getError() will contain the category of error
    * - getErrorMessage() will contain descriptive information
    * 
    * @sa clearErrors() 
    */
    virtual bool hasFailed() const = 0;

    /**
    * @brief Get last error code
    */
    virtual WrapperErrorCode getError() const = 0;

    /**
    * @brief Get last error message
    */
    virtual std::string getErrorMessage() = 0;

    /**
    * @brief Clear error status
    */
    virtual void clearError() = 0;

    /**
    * @brief Release memory allocation
    * 
    * User of this API should not call delete for objects,
    * instead call this method to perform the deallocation
    * from inside the library.
    */
    virtual void Release() = 0;
};


enum class TreeGraphStyle
{
    All_Graphs_LabelRelations = -1,
    All_Graphs = 0,
    Normal_DirGraph = 1,
    RankdirLR_DirGraph = 2,
    FolderSyle_DirGraph = 3,
};


/**
* @brief Helper class to create graphical representation of a IStep3D wrapper
* 
* Creates graphs by using the DOT application.
*/
class STEP3D_DLLAPI ITreeGraphGenerator_Wrapper
{
public:
    /**
    * @brief Generate image of the Step3D tree
    * @param[in] wrapper Step3D wrapper instance
    * @param[in] mode style of graph to create
    * 
    * Uses the current nodes and relations to construct the 
    */
    virtual bool generate(IStep3D_Wrapper* wrapper, TreeGraphStyle mode) = 0;

    /**
    * @brief Release memory allocation
    * 
    * User of this API should not call delete for objects,
    * instead call this method to perform the deallocation
    * from inside the library.
    */
    virtual void Release() = 0;
};


/////////////////////////////////////////////////////////////
// Object creation
//
// ATTENTION: call object->Release() to clean the memory.
/////////////////////////////////////////////////////////////


/**
* @brief Create instance of IStep3D_Wrapper
* @note Do not make a delete on this object, instead
* call the IStep3D_Wrapper::Release() method.
*/
STEP3D_DLLAPI IStep3D_Wrapper* CreateIStep3D_Wrapper();

/**
* @brief Create instance of ITreeGraphGenerator_Wrapper
* @note Do not make a delete on this object, instead
* call the ITreeGraphGenerator_Wrapper::Release() method.
*/
STEP3D_DLLAPI ITreeGraphGenerator_Wrapper* CreateITreeGraphGenerator_Wrapper();
