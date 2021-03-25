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

#pragma once

/**
* Implement Public interface of the STEP-3D Wrapper library
* 
* Linked to Stepcode shared libraries.
*/
#include "step3d_wrapper.h"


class TreeGraphGenerator_Wrapper_Imp: public ITreeGraphGenerator_Wrapper
{
public:
    TreeGraphGenerator_Wrapper_Imp();

    virtual ~TreeGraphGenerator_Wrapper_Imp();

    /**
    * @brief Create an image representing the HLR structure
    * @param[in] wrapper Step 3D view (nodes and relations)
    * @param[in] mode DOT graph creation style
    * 
    * Example:
    * @verbatim
    * digraph G {
    * label="MyParts.step"
    * 
    * node [fontname="Courier New", fontsize=8];
    * 
    * node [shape=box];
    * PD5 [label="Part #10"];
    * PD380 [label="SubPart #385"];
    * 
    * node [shape=ellipse,fillcolor=gray,style=filled];
    * 
    * PD367 [label="Caja #29"];
    * PD737 [label="Cube #399"];
    * PD854 [label="Cylinder #748"];
    * 
    * PD5 -> PD367;
    * PD5 -> PD380;
    * PD380 -> PD737;
    * PD380 -> PD854;
    * }
    * @endverbatim
    */
    virtual bool generate(IStep3D_Wrapper* wrapper, TreeGraphStyle mode) override;

    virtual void Release() override;

protected:
    bool m_dot_relation_labeled;
    std::string m_filename;
    std::list<Part_Wrapper> m_nodes;
    std::list<Relation_Wrapper> m_relations;

    bool buildDOT(TreeGraphStyle dottype);
};