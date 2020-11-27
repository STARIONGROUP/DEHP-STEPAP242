#include <string>
#include <iostream>

#include "step3d_wrapper.h"

using namespace std;


/**
* @brief Process information in a STEP-3D file
* 
* Show content (nodes and relations) and generate tree graph.
*/
void processStep3DFile(std::string fname, bool drawGraph)
{
    auto wrapper = CreateIStep3D_Wrapper();

    if (wrapper->load(fname))
    {
        wrapper->parseHLRInformation();
    }

    if (wrapper->hasFailed())
    {
        cout << "ERROR " << (int)wrapper->getError() << ": " << wrapper->getErrorMessage();
        return;
    }

    cout << endl;

    auto hdr = wrapper->getHeaderInfo();
    
    cout << "File_Description: " << endl;
    cout << "   " << hdr.file_description.description << endl;
    cout << "   " << hdr.file_description.implementation_level << endl;
    cout << "File_Name:" << endl;
    cout << "   " << hdr.file_name.name << endl;
    cout << "   " << hdr.file_name.time_stamp << endl;
    cout << "   " << hdr.file_name.author << endl;
    cout << "   " << hdr.file_name.organization << endl;
    cout << "   " << hdr.file_name.preprocessor_version << endl;
    cout << "   " << hdr.file_name.originating_system << endl;
    cout << "   " << hdr.file_name.authorisation << endl;
    cout << "File_Schema:" << endl;
    cout << "   " << hdr.file_schema << endl;

    cout << endl;
    
#ifdef test_description_ctype
    auto dc = wrapper->getDescriptionCType();
    cout << "File_Name: " << dc.File_Name << endl;
    cout << "File_Description: " << dc.File_Description << endl;
    cout << "File_Description_Id: " << dc.File_Description_Id << endl;
    cout << "File_Schema: " << dc.File_Schema << endl;
#endif

    std::list<Part_Wrapper> nodes = wrapper->getNodes();
    for (const Part_Wrapper& n : nodes)
    {
        cout << "Node #" << n.id << " " << n.type << " " << n.name << endl;
        
        cout << " --> placement.name: " << n.placement.name << endl;

        cout << " --> placement.location: [" << n.placement.location[0]
            << ", " << n.placement.location[1]
            << ", " << n.placement.location[2]
            << "]" << endl;

        cout << " --> placement.axis: [" << n.placement.axis[0]
            << ", " << n.placement.axis[1]
            << ", " << n.placement.axis[2]
            << "]" << endl;

        cout << " --> placement.ref_direction: [" << n.placement.ref_direction[0]
            << ", " << n.placement.ref_direction[1]
            << ", " << n.placement.ref_direction[2]
            << "]" << endl;

        cout << " --> representation_type: " << n.representation_type << endl;
        cout << endl;
    }

    auto relations = wrapper->getRelations();
    for (const auto& n : relations)
    {
        cout << "Relation #" << n.id << " " << n.type << " " << n.name << " for #" << n.relating_id << " --> #" << n.related_id << endl;
    }

    if (drawGraph)
    {
        cout << endl;

        auto graphGenerator = CreateITreeGraphGenerator_Wrapper();
        TreeGraphStyle graphtype = TreeGraphStyle::All_Graphs;

        graphGenerator->generate(wrapper, graphtype);
        graphGenerator->Release();
    }

    wrapper->Release();
}

int main(int argc, char* argv[]) 
{
    // Examples of arguments:
    // ----------------------
    // "D:\dev\DEHP\DEHP-Stepcode\stepcode\extra\step3d_wrapper_test\examples\dm1-id-214.stp"
    // "D:\dev\DEHP\DEHP-Stepcode\stepcode\extra\step3d_wrapper_test\examples\MyParts.step"
    // "D:\dev\DEHP\SharePoint\Project Documents\XIPE_STEP_3D_Samples\MyParts.step"
    // "D:\dev\DEHP\SharePoint\Project Documents\XIPE_STEP_3D_Samples\XIPE_all_v1.stp"

    cout << "Stepcode version: " << getStepcodeVersion() << endl;

    bool drawGraph = false;

    for (int i=1; i<argc; ++i)
    {
        const string option(argv[i]);

        if (option == "--dot")
        {
            drawGraph = true;
            continue;
        }

        processStep3DFile(option, drawGraph);
    }

    return 0;
}
