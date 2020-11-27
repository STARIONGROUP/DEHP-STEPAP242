#pragma once

#include "Step3D_Wrapper_Imp.h"

#include "SdaiHeaderSchema.h"


#include <iostream>
#include <fstream>
using namespace std;


// Initialize static members
const std::string Step3D_Wrapper_Imp::HdrFD("File_Description");
const std::string Step3D_Wrapper_Imp::HdrFN("File_Name");
const std::string Step3D_Wrapper_Imp::HdrFS("File_Schema");

const string Step3D_Wrapper_Imp::PD("Product_Definition");
const string Step3D_Wrapper_Imp::SR("Shape_Representation");
const string Step3D_Wrapper_Imp::ABSR("Advanced_Brep_Shape_Representation");
const string Step3D_Wrapper_Imp::NAUO("Next_Assembly_Usage_Occurrence");
const string Step3D_Wrapper_Imp::SDR("Shape_Definition_Representation");


class WrapperException
{
public:
    WrapperException(int errcode, const std::string message) : code((WrapperErrorCode)errcode), msg(message) {}
    WrapperException(WrapperErrorCode errcode, const std::string message) : code(errcode), msg(message) {}

    WrapperErrorCode code;
    std::string msg;
};


std::string PrettyPrintAttributeType(BASE_TYPE type) {
    std::string ret = "";
    switch(type) {
        case INTEGER_TYPE:
            ret = "INTEGER_TYPE"; break;
        case REAL_TYPE:
            ret = "REAL_TYPE"; break;
        case BOOLEAN_TYPE:
            ret = "BOOLEAN_TYPE"; break;
        case LOGICAL_TYPE:
            ret = "LOGICAL_TYPE"; break;
        case STRING_TYPE:
            ret = "STRING_TYPE"; break;
        case BINARY_TYPE:
            ret = "BINARY_TYPE"; break;
        case ENUM_TYPE:
            ret = "ENUM_TYPE"; break;
        case SELECT_TYPE:
            ret = "SELECT_TYPE"; break;
        case ENTITY_TYPE:
            ret = "ENTITY_TYPE"; break;
        case AGGREGATE_TYPE:
            ret = "AGGREGATE_TYPE"; break;
        case NUMBER_TYPE:
            ret = "NUMBER_TYPE"; break;
        case ARRAY_TYPE:
            ret = "ARRAY_TYPE"; break;
        case BAG_TYPE:
            ret = "BAG_TYPE"; break;
        case SET_TYPE:
            ret = "SET_TYPE"; break;
        case LIST_TYPE:
            ret = "LIST_TYPE"; break;
        case GENERIC_TYPE:
            ret = "GENERIC_TYPE"; break;
        case REFERENCE_TYPE:
            ret = "REFERENCE_TYPE"; break;
        case UNKNOWN_TYPE:
            ret = "UNKNOWN_TYPE"; break;
    }

    return ret;
}

void PrintInstance(SDAI_Application_instance* instance)
{
    std::cout << "EntityName: " << instance->EntityName() << std::endl;
    std::cout << "StepFileId: " << instance->StepFileId() << std::endl;
    //std::cout << "Comment: " << instance->P21Comment() << std::endl;
            
    const EntityDescriptor* entityDescriptor = instance->getEDesc();
    std::cout << "Subtypes: " << entityDescriptor->Subtypes().EntryCount() << std::endl;
    std::cout << "Supertypes: " << entityDescriptor->Supertypes().EntryCount() << std::endl;
    std::cout << "ExplicitAttr: " << entityDescriptor->ExplicitAttr().EntryCount() << std::endl;
    std::cout << "InverseAttr: " << entityDescriptor->InverseAttr().EntryCount() << std::endl;

    int attributeCount = instance->AttributeCount();
    std::cout << "AttributeCount: " << attributeCount << std::endl;

    //if (applicationInstance->GetInstanceTypeName() 
    for( int i = 0; i < attributeCount; i++ )
    {
        STEPattribute* attribute = &instance->attributes[i];
        std::cout << "Attribute" << i+1 << ":" << std::endl;
        std::cout << "\tName:       " << attribute->Name() << std::endl;
        std::cout << "\tNonRefType: " << PrettyPrintAttributeType(attribute->NonRefType()) << std::endl;
        int refCount = attribute->getRefCount();
        std::cout << "\tRefCount:   " << refCount << std::endl;
    }
}

void PrintInstanceShort(SDAI_Application_instance* instance)
{
    if (instance)
    {
        // Ref: D:\dev\DEHP\DEHP-Stepcode\stepcode\src\clstepcore\entityDescriptor.h
        cout << "EntityName: " << instance->EntityName() << " #" << instance->StepFileId() << endl;
    }
    else
    {
        cout << "PrintInstanceShort(nullptr)" << endl;
    }
}



Step3D_Wrapper_Imp::Step3D_Wrapper_Imp()
{
    m_instancelist = nullptr;
    m_registry = nullptr;
    m_stepfile = nullptr;

    m_errorCode = WrapperErrorCode::NO_ERROR;
}

Step3D_Wrapper_Imp::~Step3D_Wrapper_Imp()
{
    cout << "~Step3D_Wrapper_Imp" << endl;

    delete m_instancelist;
    delete m_stepfile;
    delete m_registry;
}

bool Step3D_Wrapper_Imp::load(std::string fname)
{
    m_filename = fname;

    clearError();

    checkFileToLoad();
    if (hasFailed()) return false;

#ifdef __demo_wrapper__
    return true;
#else
    int ownsInstanceMemory = 1;
    m_instancelist = new InstMgr(ownsInstanceMemory);
    m_registry = new Registry(SchemaInit);
    m_stepfile = new STEPfile(*m_registry, *m_instancelist);

    try
    {
        Severity sev = m_stepfile->ReadExchangeFile(m_filename.c_str());

#ifndef NDEBUG
        cout << "Severity: " << sev << endl;
        ErrorDescriptor errorDesc = m_stepfile->Error();
        cout << "ED: " << errorDesc.severityString() << endl;
        cout << "ED: " << errorDesc.DetailMsg() << endl;
#endif

        if (sev < SEVERITY_WARNING)  // non-recoverable error
        {
            m_errorCode = WrapperErrorCode::FILE_READ;
            
            std::stringstream ss;
            ss << "Error reading the STEP file content: " << m_stepfile->Error().severityString();

            m_errorMessage = ss.str();
            return false;
        }
    }
    catch( std::exception &e )
    {
        std::cerr << e.what() << std::endl;
        
        m_errorCode = WrapperErrorCode::FILE_READ;
        m_errorMessage = e.what();

        return false;
    }

    return true;
#endif
}

std::string Step3D_Wrapper_Imp::getFilename()
{
    return m_filename;
}

bool Step3D_Wrapper_Imp::parseHLRInformation()
{
    cout << "Getting the HLR related information" << endl;

    if (hasFailed()) return false; // avoid parsing when the current state has errors (from load)

    if (m_stepfile == nullptr)
    {
        m_errorCode = WrapperErrorCode::FILE_NOT_FOUND;
        m_errorMessage = "No loaded file yet, parse content is not possible";
        return false;
    }

    try
    {
        processHeader();

        processContent();

        processGeometricInformation();

        cout << "Parsing content finished!" << endl;
    }
    catch (WrapperException& e)
    {
        m_errorCode = e.code;
        m_errorMessage = e.msg;
        cout << "Parsing content finished with errors!" << endl;
    }

    return !hasFailed();
}

Step3D_HeaderInfo_Wrapper Step3D_Wrapper_Imp::getHeaderInfo()
{
    return m_headerInfo;
}

std::list<Part_Wrapper> Step3D_Wrapper_Imp::getNodes()
{
    return m_nodes;
}

std::list<Relation_Wrapper> Step3D_Wrapper_Imp::getRelations()
{
    return m_relations;
}

bool Step3D_Wrapper_Imp::hasFailed() const
{
    return m_errorCode != WrapperErrorCode::NO_ERROR;
}

WrapperErrorCode Step3D_Wrapper_Imp::getError() const
{
    return m_errorCode;
}

void Step3D_Wrapper_Imp::clearError()
{
    m_errorCode = WrapperErrorCode::NO_ERROR;
    m_errorMessage.clear();
}

std::string Step3D_Wrapper_Imp::getErrorMessage()
{
    return m_errorMessage;
}

void Step3D_Wrapper_Imp::Release()
{
    delete this;
}


///////////////////////////////////
// STEP-3D methods
///////////////////////////////////

void Step3D_Wrapper_Imp::processHeader()
{
    cout << "Parsing header..." << endl;

    try
    {
        auto headerMgr = m_stepfile->HeaderInstances();

        const int count = headerMgr->InstanceCount();

        for (int i = 0; i < count; i++)
        {
            MgrNode* node = headerMgr->GetMgrNode(i);
            SDAI_Application_instance* instance = node->GetApplication_instance();

            const string eName(instance->EntityName());

            // EntityName: File_Description #1
            // EntityName: File_Name #2
            // EntityName: File_Schema #3
            //PrintInstanceShort(instance);

            //if (applicationInstance->IsInstanceOf("File_Description"))
            //{
            //    cout << "instance " << applicationInstance->EntityName() << " isOf File_Description" << endl;
            //}
            //instance->getEDesc()

            SdaiFile_description* fdesc = dynamic_cast<SdaiFile_description*>(instance);
            SdaiFile_name* fname = dynamic_cast<SdaiFile_name*>(instance);
            SdaiFile_schema* fschema = dynamic_cast<SdaiFile_schema*>(instance);

            if (fdesc)
            {
                auto desc = fdesc->description_();
                auto implevel = fdesc->implementation_level_();

                desc->asStr(m_headerInfo.file_description.description);
                m_headerInfo.file_description.implementation_level = implevel.c_str();
            }

            if (fname)
            {
                m_headerInfo.file_name.name = fname->name_().c_str();
                m_headerInfo.file_name.time_stamp = fname->time_stamp_().c_str();
                fname->author_()->asStr(m_headerInfo.file_name.author);
                fname->organization_()->asStr(m_headerInfo.file_name.organization);
                m_headerInfo.file_name.preprocessor_version = fname->preprocessor_version_().c_str();
                m_headerInfo.file_name.originating_system = fname->originating_system_().c_str();
                m_headerInfo.file_name.authorisation = fname->authorization_().c_str();
            }

            if (fschema)
            {
                fschema->schema_identifiers_()->asStr(m_headerInfo.file_schema);
            }

            // Two different ways to compare the type
            // SdaiFile_description* fd = dynamic_cast<SdaiFile_description*>(instance);
            //if (fd)
            //{
            //    cout << "is fd" << endl;
            //}
            //
            //if (eName == HdrFD)
            //{
            //    cout << "is HdrFN" << endl;
            //}

            //if (applicationInstance->EntityName() == "D:\dev\DEHP\DEHP-Stepcode\stepcode\src\clstepcore\entityDescriptor.h")
        }
    }
    catch (std::exception &e)
    {
        std::cerr << e.what() << std::endl;
        
        WrapperException wex(WrapperErrorCode::FILE_PROCESS, string(e.what()) + " at Step3D_Wrapper_Imp::processHeader()");
        throw wex;
    }
    catch (...)
    {
        std::cerr << "unnown exception" << std::endl;
        
        WrapperException wex(WrapperErrorCode::FILE_PROCESS, "Unknown Exception at Step3D_Wrapper_Imp::processHeader()");
        throw wex;
    }
}

void Step3D_Wrapper_Imp::processContent()
{
    cout << "Parsing content..." << endl;

    try
    {
        MgrNode* node = nullptr;
        SDAI_Application_instance* applicationInstance = nullptr;

        for (int i = 0; i < m_instancelist->InstanceCount(); i++)
        {
            node = m_instancelist->GetMgrNode(i);
            applicationInstance = node->GetApplication_instance();

            //PrintInstance(applicationInstance);

            string eName(applicationInstance->EntityName());

            if (eName == PD)
            {
                processPD(applicationInstance);

                //PrintPD(applicationInstance);
                // 
                // getShapeRepresentationFromPD(static_cast<SdaiProduct_definition*>(applicationInstance));
                // std::cout << std::endl;
            }
            else if (eName == NAUO)
            {
                //PrintNAUO(applicationInstance);
                processNAUO(applicationInstance);
                //std::cout << std::endl;
            }
            else if (eName == SDR)
            {
                //PrintNAUO(applicationInstance);
                processSDR(applicationInstance);
                //std::cout << std::endl;
            }
            //else if (eName == ABSR)
            //{
            //    PrintInstance(applicationInstance);
            //    std::cout << std::endl;
            //}
            //else if (endsWith(eName, "_Shape_Representation"))
            //{
            //    continue;
            //    std::cout << "ANOTHER" << std::endl;
            //    std::cout << "  EntityName: " << applicationInstance->EntityName() << std::endl;
            //    std::cout << "  StepFileId: " << applicationInstance->StepFileId() << std::endl;
            //    std::cout << std::endl;
            //}
        }
    }
    catch (std::exception &e)
    {
        std::cerr << e.what() << std::endl;
        
        WrapperException wex(WrapperErrorCode::FILE_PROCESS, string(e.what()) + " at Step3D_Wrapper_Imp::processContent()");
        throw wex;
    }
}

void Step3D_Wrapper_Imp::processGeometricInformation()
{
    cout << "Parsing geometric information..." << endl;

    // 1) Nodes position
    for (auto& node : m_nodes)
    {
        auto mgrnode = m_instancelist->FindFileId(node.id);
        auto instance = mgrnode->GetApplication_instance();
        //PrintInstanceShort(instance);

        SdaiProduct_definition* pd = dynamic_cast<SdaiProduct_definition*>(instance);
        //PrintInstanceShort(pd);

        // 1) Get the SDP
        if (m_PD2SDR_map.find(pd) == m_PD2SDR_map.end())
        {
            m_errorCode = WrapperErrorCode::FILE_PROCESS;
            stringstream ss;
            ss << "Not PD in m_PD2SDR_map[" << node.id << + "]";
            m_errorMessage = ss.str();
            return;
        }

        auto sdp = m_PD2SDR_map[pd];
        PrintInstanceShort(sdp);

        // 2) Go to the Representation
        //auto ur = sdp->used_representation_(); -- > gives error
        auto ur = sdp->property_definition_representation_used_representation_();
        
        if (!ur) continue;

        SdaiShape_representation* sr = dynamic_cast<SdaiShape_representation*>(ur);
        SdaiAdvanced_brep_shape_representation* absr = dynamic_cast<SdaiAdvanced_brep_shape_representation*>(ur);

        if (absr)
        {
            PrintInstanceShort(absr);
        }
        else if (sr)
        {
            PrintInstanceShort(sr);
        }
        else continue;

        node.representation_type = ur->EntityName();

        auto items = ur->items_();
        auto ih = items->GetHead();

        // NOTE: we expect the Placement as the first item
        auto eNode = dynamic_cast<EntityNode*>(ih);
        auto placementInstance = eNode->node;
        PrintInstanceShort(placementInstance);

        processAxis2PLacement3D(placementInstance, node.placement);

#ifdef  show_all_ur_items
        SingleLinkNode* singleln = ih;
        const int sz = items->EntryCount();
        cout << "items->EntryCount() = " << sz << endl;

        for (int j = 0; j < sz; j++)
        {
            auto entityNode = dynamic_cast<EntityNode*>(singleln);
            auto ai = entityNode->node;
            PrintInstanceShort(ai);
            singleln = singleln->NextNode();
        }

        cout << endl;
        /*
        EntityName: Shape_Definition_Representation #378
        EntityName: Shape_Representation #385
        items->EntryCount() = 3
        EntityName: Axis2_Placement_3d #11
        EntityName: Axis2_Placement_3d #386
        EntityName: Axis2_Placement_3d #390

        EntityName: Shape_Definition_Representation #735
        EntityName: Advanced_Brep_Shape_Representation #399
        items->EntryCount() = 2
        EntityName: Axis2_Placement_3d #11
        EntityName: Manifold_Solid_Brep #400

        EntityName: Shape_Definition_Representation #852
        EntityName: Advanced_Brep_Shape_Representation #748
        items->EntryCount() = 2
        EntityName: Axis2_Placement_3d #11
        EntityName: Manifold_Solid_Brep #749
        */
#endif //  show_all_ur_items
    }


    // 2) Relations position
    // Two *Transformation* types:
    // - Item_Defined_Transformation
    // - Functionally_Defined_Transformation
    //
    // Manage only Item_Defined_Transformation, the other is not used in reference cases
    //
    // A
    //TODO: find transformation 




    // 3) Node occurence absolute position
    // To perform this calcul, a real tree must be constructed
    // in order to perform the correct sequence of transformations
    // from parent to child.
    //
    // Has the bounding box needs to be calculated here?
    //
    // Note that depending how the tree view shows multiple usages
    // will have a unique position value for all the group.
    //
    // The
    //
    // NOTE: this seems to be more complex than expected.
    //
    // TODO: check if absolute position attributes must be added here or calculated by the user
}

void Step3D_Wrapper_Imp::processPD(SDAI_Application_instance* instance)
{
    SdaiProduct_definition* pd = static_cast<SdaiProduct_definition*>(instance);

    // Get associated PRODUCT to retrieve the product's name
    auto f = pd->formation_();
    auto p = f->of_product_();

    Part_Wrapper node;
    node.id = pd->StepFileId();
    node.type = "PD"; // pd->EntityName();
    node.name = p->name_().c_str();

    cout << "PD #" << node.id << " " << node.name << "" << endl;

    m_nodes.push_back(node);

    //getShapeRepresentationFromPD(pd);
}

void Step3D_Wrapper_Imp::processNAUO(SDAI_Application_instance* instance)
{
    SdaiNext_assembly_usage_occurrence* nauo = static_cast<SdaiNext_assembly_usage_occurrence*>(instance);
    
    // Get selected PRODUCT_DEFINITIONS, ignore any other kind of usages
    auto relating = nauo->relating_product_definition_();
    auto related = nauo->related_product_definition_();

    SdaiProduct_definition* relating_pd = nullptr;
    SdaiProduct_definition* related_pd = nullptr;

    if (relating->IsProduct_definition())
    {
        relating_pd = relating->operator SdaiProduct_definition_ptr();
    }
    else return; // case not managed

    if (related->IsProduct_definition())
    {
        related_pd = related->operator SdaiProduct_definition_ptr();
    }
    else return; // case not managed

    cout << "NAUO #" << nauo->StepFileId() << " (#" << relating_pd->StepFileId() << ", #" << related_pd->StepFileId() << ")" << endl;
    //// NAUO #376 (#'design', #'design')
    //cout << "NAUO #" << nauo->StepFileId() << " (#" << related->id_().c_str() << ", #" << relating->id_().c_str() << ")" << endl;

    Relation_Wrapper relation;
    relation.id = nauo->StepFileId();
    relation.type = "NUAO"; // nauo->EntityName();
    relation.name = nauo->name_().c_str();
    relation.relating_id = relating_pd->StepFileId();
    relation.related_id = related_pd->StepFileId();


    // Seach for the CDSR.PDS.definition == this NAUO
    // to retrieve the transformation's data


    m_relations.push_back(relation);
}

void Step3D_Wrapper_Imp::processSDR(SDAI_Application_instance* instance)
{
    SdaiShape_definition_representation* sdr = dynamic_cast<SdaiShape_definition_representation*>(instance);

    cout << "processSDR SDR" << endl;
    PrintInstanceShort(instance);

    auto sdr_rep_definition = sdr->property_definition_representation_definition_();
    PrintInstanceShort(sdr_rep_definition);

    SdaiProduct_definition_shape* pds = dynamic_cast<SdaiProduct_definition_shape*>(sdr_rep_definition);
    if (pds)
    {
        auto pds_def = pds->definition_();

        if (pds_def->IsCharacterized_product_definition())
        {
            auto cpd = pds_def->operator SdaiCharacterized_product_definition_ptr();

            if (cpd->IsProduct_definition())
            {
                auto pd = cpd->operator SdaiProduct_definition_ptr();

                cout << "SDR --> PD found" << endl;
                PrintInstanceShort(sdr);
                PrintInstanceShort(pd);
                cout << endl;

                m_PD2SDR_map[pd] = sdr;
            }
        }
    }
}

void Step3D_Wrapper_Imp::processAxis2PLacement3D(SDAI_Application_instance* instance, Axis2_Placement_3d_Wrapper& placement)
{
    SdaiAxis2_placement_3d* pos = dynamic_cast<SdaiAxis2_placement_3d*>(instance);

    if (pos == nullptr)
    {
        placement.name = "ERROR";

        // silently continue, no expection
        m_errorCode = WrapperErrorCode::FILE_PROCESS;
        m_errorMessage = "Step3D_Wrapper_Imp::processAxis2PLacement3D(nullptr)";
        return;
    }

    auto location = pos->location_();
    auto axis = pos->axis_();
    auto ref_direction = pos->ref_direction_();

    placement.name = pos->name_().c_str();  // generally is empty for internal positions

    processCartesianPoint(location, placement.location);
   
    processDirection(ref_direction, placement.axis);

    processDirection(ref_direction, placement.ref_direction);
}

void Step3D_Wrapper_Imp::processCartesianPoint(SdaiCartesian_point* instance, CartesianPoint_Wrapper& point)
{
    RealAggregate* coord = instance->coordinates_();
    
    cout << instance->EntityName() << " #" << instance->StepFileId() << " EntryCount() = " << coord->EntryCount() << endl;

    SingleLinkNode* link = coord->GetHead();

    for (int i = 0; i < 3; i++)
    {
        auto eNode = dynamic_cast<RealNode*>(link);
        point[i] = eNode->value;
        link = link->NextNode();
    }
}

void Step3D_Wrapper_Imp::processDirection(SdaiDirection* instance, Direction_Wrapper& direction)
{
    RealAggregate* ratios = instance->direction_ratios_();

    cout << instance->EntityName() << " EntryCount() = " << ratios->EntryCount() << endl;

    SingleLinkNode* link = ratios->GetHead();

    for (int i = 0; i < 3; i++)
    {
        auto eNode = dynamic_cast<RealNode*>(link);
        direction[i] = eNode->value;
        link = link->NextNode();
    }
}

SDAI_Application_instance* Step3D_Wrapper_Imp::findEntityAttribute(SDAI_Application_instance* instance, const std::string& name)
{
    int attributeCount = instance->AttributeCount();

    for( int i = 0; i < attributeCount; i++ )
    {
        STEPattribute* attribute = &instance->attributes[i];
        string attributeName(attribute->Name());

        if (attribute->NonRefType() == ENTITY_TYPE)
        {
            if (attributeName == name)
            {
                return attribute->Entity();
            }
        }
    }

    std::cerr << "ENTITY_TYPE from name '" << name << "' not found!" << std::endl;
    m_errorCode = WrapperErrorCode::FILE_PROCESS;
    m_errorMessage = "ENTITY_TYPE from name '" + name + "' not found!";

    return nullptr;
}

SdaiShape_representation* Step3D_Wrapper_Imp::getShapeRepresentationFromPD(SdaiProduct_definition* pd)
{
    if (pd == nullptr)
    {
        cerr << "getShapeRepresentationFromPD(nullptr);" << endl;
        m_errorCode = WrapperErrorCode::FILE_PROCESS;
        m_errorMessage = "getShapeRepresentationFromPD(nullptr)";
        return nullptr;
    }
    

    cout << "-----------------------------------" << endl;
    cout << "getShapeRepresentationFromPD()";
    PrintInstanceShort(pd);
    cout << "-----------------------------------" << endl;

    //const SDAI_Application_instance::iAMap_t& iattrs = pd->getInvAttrs();
    //
    //cout << "Inverse attributes: " << iattrs.size() << endl;
    //for (auto const& x : iattrs)
    //{
    //    cout << x.first  // string (key)
    //         << ':'
    //         << x.second.i->EntityName() // string's value 
    //         << std::endl ;
    //}

    // Link composition:
    // SDR --> PDS --> PD
    // once SDR is located:
    // SDR --> SD | ABSR

    // Loop for all instances to find the SdaiShape_representation

    MgrNode* node = nullptr;
    SDAI_Application_instance* instance = nullptr;

    for (int i = 0; i < m_instancelist->InstanceCount(); i++)
    {
        node = m_instancelist->GetMgrNode(i);
        instance = node->GetApplication_instance();

        //PrintInstance(applicationInstance);

        SdaiShape_definition_representation* sdr = dynamic_cast<SdaiShape_definition_representation*>(instance);

        if (sdr)
        {
            cout << "Check SDR" << endl;
            PrintInstanceShort(instance);

            auto sdr_rep_definition = sdr->property_definition_representation_definition_();
            PrintInstanceShort(sdr_rep_definition);

            //pdrd->IsSDAIKindOf("");
            SdaiProduct_definition_shape* pds = dynamic_cast<SdaiProduct_definition_shape*>(sdr_rep_definition);
            if (pds)
            {
                auto pds_def = pds->definition_();

                if (pds_def->IsCharacterized_product_definition())
                {
                    auto cpd = pds_def->operator SdaiCharacterized_product_definition_ptr();

                    if (cpd->IsProduct_definition())
                    {
                        auto _pd = cpd->operator SdaiProduct_definition_ptr();

                        cout << "From PD" << endl;
                        PrintInstanceShort(instance);
                        cout << "We found the PD" << endl;
                        PrintInstanceShort(_pd);
                        cout << "In SDR" << endl;
                        PrintInstanceShort(sdr);

                        if (pd == _pd)
                        {
                            cout << "!!! SDR found for input PD" << endl;
                            PrintInstanceShort(sdr);
                            PrintInstanceShort(pd);
                            break;
                        }
                    }
                }
            }

//#define _old_test_
#ifdef _old_test_
            auto sdr_definition = sdr->definition_();

            if (sdr_definition->Isch())
            {
                sdr->pro
            }

            if (sdr_definition->IsProperty...()) //
            {
                cout << "definition->IsProperty_definition()" << endl;

                //PrintInstance(sdr_definition);

                auto prop_def = sdr_definition->operator SdaiProperty_definition_ptr();

                auto prop_def_definition = prop_def->definition_();

                if (prop_def_definition->IsCharacterized_product_definition())
                {
                    auto char_prod_def = prop_def_definition->operator SdaiCharacterized_product_definition_ptr();
                    
                    if (char_prod_def->IsProduct_definition())
                    {
                        auto _pd = char_prod_def->operator SdaiProduct_definition_ptr();

                        cout << "From PD" << endl;
                        PrintInstanceShort(instance);
                        cout << "We found the PD" << endl;
                        PrintInstanceShort(_pd);
                        cout << "In SDR" << endl;
                        PrintInstanceShort(sdr);
                    }
                }
            }
            else
            {
                cout << "definition->IsProperty_definition() FALSE" << endl;
            }
#endif
        }
    }

    return nullptr;
}


///////////////////////////////////
// Helpers
///////////////////////////////////
bool Step3D_Wrapper_Imp::endsWith(const std::string& str, const std::string& suffix)
{
    return str.size() >= suffix.size() && 0 == str.compare(str.size()-suffix.size(), suffix.size(), suffix);
}

void Step3D_Wrapper_Imp::checkFileToLoad()
{
    cout << "File to load: " << m_filename << endl;

    ifstream ifile;
    ifile.open(m_filename);
    if (!ifile.is_open())
    {
        m_errorCode = WrapperErrorCode::FILE_NOT_FOUND;
        m_errorMessage = "File does not exists: " + m_filename;

        cerr << "Step3D_Wrapper_Imp::checkFileToLoad(): " << m_errorMessage << endl;
    }
}

void Step3D_Wrapper_Imp::fillDemoData()
{
    // Load demo data
    m_headerInfo.file_name.name = m_filename;
    m_headerInfo.file_description.description = "No description";
    m_headerInfo.file_description.description = "Unknown";
    m_headerInfo.file_schema = "Unkown Schema";

//#if _MSC_VER > 1800 
//    m_nodes.push_back(Part_Wrapper() = { 3, "PD", "Root" });
//    m_nodes.push_back(Part_Wrapper() = { 11, "PD", "Box" });
//    m_nodes.push_back(Part_Wrapper() = { 7, "PD", "Triangle" });
//
//    m_relations.push_back(Relation_Wrapper() = { 100, "NAO", "Hijo", 3, 11 });
//    m_relations.push_back(Relation_Wrapper() = { 101, "NAO", "Nieto", 11, 7 });
//#else
    Part_Wrapper node;
    node.id = 3; node.type = "PD"; node.name = "Root";
    Axis2_Placement_3d_Wrapper placement1;
    placement1.location[0] = 50;
    node.placement = placement1;
    m_nodes.push_back(node);
    node.id = 11; node.type = "PD"; node.name = "Box";
    m_nodes.push_back(node);
    node.id = 7; node.type = "PD"; node.name = "Triangle";
    m_nodes.push_back(node);

    Relation_Wrapper relation;
    relation.id = 100; relation.type = "NAO"; node.name = "Hijo";
    m_relations.push_back(relation);
    relation.id = 101; relation.type = "NAO"; node.name = "Nieto";
    m_relations.push_back(relation);
//#endif
}

//#define _demo_
#ifdef _demo_
class Step3D_Wrapper_Demo : Step3D_Wrapper
{
public:
    Step3D_Wrapper_Demo();

    virtual ~Step3D_Wrapper_Demo();

    bool load(std::string fname) override
    {
        // Load demo data
        //Part_Wrapper node = { .id = 3, .type = };

        m_nodes.push_back(Part_Wrapper() = { 3, "PD", "Root" });
        m_nodes.push_back(Part_Wrapper() = { 11, "PD", "Box" });
        m_nodes.push_back(Part_Wrapper() = { 7, "PD", "Triangle" });

        m_relations.push_back(Relation_Wrapper() = { 100, "NAO", "Hijo", 3, 11 });
        m_relations.push_back(Relation_Wrapper() = { 100, "NAO", "Nieto", 11, 3 });

        return 0; // no errors
    }
};

Step3D_Wrapper_Demo::Step3D_Wrapper_Demo() {}
Step3D_Wrapper_Demo::~Step3D_Wrapper_Demo() {}
#endif
