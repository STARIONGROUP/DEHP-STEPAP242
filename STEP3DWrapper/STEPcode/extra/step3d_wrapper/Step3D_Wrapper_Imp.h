#pragma once

/**
* Implement Public interface of the STEP-3D Wrapper library
* 
* Linked to Stepcode shared libraries.
*/
#include "step3d_wrapper.h"

// STEPcode headers
#include "Registry.h"
#include "STEPfile.h"
#include "sdai.h"
#include "errordesc.h"

// From "schemas/sdai_ap242"
#include "SdaiAP242_MANAGED_MODEL_BASED_3D_ENGINEERING_MIM_LF.h"

// STL headers
#include <sstream>
#include <set>
#include <utility>


class Step3D_Wrapper_Imp: public IStep3D_Wrapper
{
public:
    Step3D_Wrapper_Imp();
    virtual ~Step3D_Wrapper_Imp();

    bool load(std::string fname) override;
    std::string getFilename() override;

    bool parseHLRInformation() override;

    Step3D_HeaderInfo_Wrapper getHeaderInfo() override;
#ifdef test_description_ctype
    TDescriptionNodeCType getDescriptionCType() override;
#endif
    std::list<Part_Wrapper> getNodes() override;
    std::list<Relation_Wrapper> getRelations() override;

    bool hasFailed() const override;
    WrapperErrorCode getError() const override;
    void clearError() override;
    std::string getErrorMessage() override;

    void Release() override;

protected:
    std::string m_filename; //!< Full path to the working file. @sa load()

    InstMgr* m_instancelist;
    Registry* m_registry;
    STEPfile* m_stepfile;

    Step3D_HeaderInfo_Wrapper m_headerInfo;
    std::list<Part_Wrapper> m_nodes;
    std::list<Relation_Wrapper> m_relations;

    // Auxiliary maps to search info
    std::map<SdaiProduct_definition*, SdaiShape_definition_representation*> m_PD2SDR_map;
    //std::list< std::pair<SdaiShape_definition_representation*, SdaiProduct_definition> > m_;

    WrapperErrorCode m_errorCode;
    std::string m_errorMessage;

    // Managed Entity Names
    static const std::string HdrFD;
    static const std::string HdrFN;
    static const std::string HdrFS;

    static const std::string PD;
    static const std::string SR;
    static const std::string ABSR;
    static const std::string NAUO;
    static const std::string SDR;

    /**
    * @brief Get the general description of the STEP file
    * 
    * The information is stored in the HEADER section which
    * is provided to any AP in a STEP file.
    */
    void processHeader();

    /**
    * @brief Get representatives of the STEP file
    * 
    * The information is stored in the DATA section which
    * is provided to any AP in a STEP file.
    */
    void processContent();

    /**
    * @brief Get geometrical information of the representative items
    * 
    * The information is stored in the DATA section which
    * is provided to any AP in a STEP file.
    */
    void processGeometricInformation();

    /**
    * @brief Add Product_Definition into list of tree nodes
    * @param[in] instance SdaiProduct_definition instance
    * 
    * @sa Part_Wrapper
    */
    void processPD(SDAI_Application_instance* instance);

    /**
    * @brief Add Next_assembly_usage_occurrence into list of tree relations
    * @param[in] instance SdaiNext_assembly_usage_occurrence instance
    * 
    * The NAUO should relate two Product_Definition instances, if not
    * the occurrence is silently ingnored.
    * 
    * @sa Relation_Wrapper
    */
    void processNAUO(SDAI_Application_instance* instance);

    /**
    * @brief Add Shape_definition_representation and its Product_Definition into the pair
    * 
    * Store the association SDP-->PD pair to speedup the location of the geometric information.
    * 
    * The getShapeRepresentationFromPD() method does an exaustive seach starting from
    * the begin of the instance list, which is a wast of time.
    */
    void processSDR(SDAI_Application_instance* instance);

    /**
    * @brief Fill the STEP entities into wrapper struct
    * @param[in] instance SdaiAxis2_Placement_3d instance
    * @param[out] placement placement information
    * 
    * It converts the AXIS2_PLACEMENT_3D, CARTESIAN_POINT, DIRECTION entities
    * into one Axis2_Placement_3d_Wrapper data.
    */
    void processAxis2PLacement3D(SDAI_Application_instance* instance, Axis2_Placement_3d_Wrapper& placement);

    /**
    * @brief Fill the STEP entitiy into wrapper struct
    * @param[in] instance SdaiCartesian_point instance
    * @param[out] point point information
    */
    void processCartesianPoint(SdaiCartesian_point* instance, CartesianPoint_Wrapper& point);

    /**
    * @brief Fill the STEP entitiy into wrapper struct
    * @param[in] instance SdaiDirection instance
    * @param[out] direction direction information
    */
    void processDirection(SdaiDirection* instance, Direction_Wrapper& direction);




    SDAI_Application_instance* findEntityAttribute(SDAI_Application_instance* instance, const std::string& name);

    SdaiShape_representation* getShapeRepresentationFromPD(SdaiProduct_definition* pd);

    // Helpers
    static bool endsWith(const std::string& str, const std::string& suffix);
    void checkFileToLoad();
    void fillDemoData();
};
