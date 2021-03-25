// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
//
//    This file is part of DEHP STEP-AP242 (STEP 3D CAD) adapter project.
// 
//    The DEHP STEP-AP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHP STEP-AP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

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
    cout << "HEADER --------------------------------" << endl;
    cout << "File_Description: " << endl;
    cout << "   description:          " << hdr.file_description.description << endl;
    cout << "   implementation_level: " << hdr.file_description.implementation_level << endl;
    cout << "File_Name:" << endl;
    cout << "   name:                 " << hdr.file_name.name << endl;
    cout << "   time_stamp:           " << hdr.file_name.time_stamp << endl;
    cout << "   author:               " << hdr.file_name.author << endl;
    cout << "   organization:         " << hdr.file_name.organization << endl;
    cout << "   preprocessor_version: " << hdr.file_name.preprocessor_version << endl;
    cout << "   originating_system:   " << hdr.file_name.originating_system << endl;
    cout << "   authorisation:        " << hdr.file_name.authorisation << endl;
    cout << "File_Schema:" << endl;
    cout << "   schema:               " << hdr.file_schema << endl;

    cout << endl;
    cout << "DATA ----------------------------------" << endl;

    std::list<Part_Wrapper> nodes = wrapper->getNodes();
    for (const Part_Wrapper& n : nodes)
    {
        cout << "Node #" << n.stepId << " " << n.type << " " << n.name << endl;
        
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
        cout << "Relation #" << n.stepId << " " << n.type << " " << n.id << " for #" << n.relating_id << " --> #" << n.related_id << endl;
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
