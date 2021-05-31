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

#include "pch.h"
#include "STEP3DFile.h"


#include "Tools/Tools.h"

using namespace STEP3DAdapter;

STEP3DFile::STEP3DFile(String^ fileName)
{
    initializeEmpty();

    m_wrapper = CreateIStep3D_Wrapper();

    if (m_wrapper->load(STEP3DAdapter::Tools::toStdString(fileName)))
    {
        if (m_wrapper->parseHLRInformation())
        {
            // Convert into managed objects
            convertHeaderInfo();
            convertParts();
            convertPartRelations();
        }
        else
        {
            // TODO: exception? user checks HasFailed
        }
    }
    else
    {
        // TODO: exception? user checks HasFailed
    }
}

STEP3DFile::~STEP3DFile() 
{
    m_wrapper->Release();
}

STEP3DFile::!STEP3DFile()
{
    m_wrapper->Release();
}

void STEP3DAdapter::STEP3DFile::initializeEmpty()
{
    m_headerInfo = gcnew STEP3D_HeaderInfo();
    m_parts = gcnew array<STEP3D_Part^>(0);
    m_relations = gcnew array<STEP3D_PartRelation^>(0);
}

void STEP3DAdapter::STEP3DFile::convertHeaderInfo()
{
    auto hi = m_wrapper->getHeaderInfo();

    STEP3D_HeaderInfo^ info = gcnew STEP3D_HeaderInfo();

    info->file_description = gcnew STEP3D_File_Description();
    info->file_name        = gcnew STEP3D_File_Name();
    info->file_schema      = gcnew String("");

    // NOTE:
    // Some fields are a list and not a simple string, i.e.
    // file_description.description
    // file_name.author
    //
    // TODO: convert string into array of strings (LOW)

    info->file_description->description          = Tools::toCleanString(hi.file_description.description);
    info->file_description->implementation_level = Tools::toUnquotedString(hi.file_description.implementation_level);
    //info->file_description->implementation_level = gcnew String(hi.file_description.implementation_level.c_str());

    info->file_name->name                  = Tools::toUnquotedString(hi.file_name.name);
    info->file_name->time_stamp            = Tools::toUnquotedString(hi.file_name.time_stamp);
    info->file_name->author                = Tools::toCleanString(hi.file_name.author);
    info->file_name->organization          = Tools::toCleanString(hi.file_name.organization);
    info->file_name->preprocessor_version  = Tools::toUnquotedString(hi.file_name.preprocessor_version);
    info->file_name->originating_system    = Tools::toCleanString(hi.file_name.originating_system);
    info->file_name->authorisation         = Tools::toUnquotedString(hi.file_name.authorisation);
    
    info->file_schema = Tools::toCleanString(hi.file_schema);

    m_headerInfo = info;
}

void STEP3DAdapter::STEP3DFile::convertParts()
{
    auto nodes = m_wrapper->getNodes();
    const int sz = (int)nodes.size();

    array<STEP3D_Part^>^ parts = gcnew array<STEP3D_Part^>(sz);

    int i = 0;
    for (const Part_Wrapper& pw: nodes)
    {
        parts[i++] = createPart(pw);
    }

    m_parts = parts;
}

void STEP3DAdapter::STEP3DFile::convertPartRelations()
{
    auto relations = m_wrapper->getRelations();
    const int sz = (int)relations.size();

    array<STEP3D_PartRelation^>^ partRelations = gcnew array<STEP3D_PartRelation^>(sz);

    int i = 0;
    for (const Relation_Wrapper& item: relations)
    {
        partRelations[i++] = createRelationPart(item);
    }

    m_relations = partRelations;
}

STEP3D_Part^ STEP3DAdapter::STEP3DFile::createPart(const Part_Wrapper& pw)
{
    STEP3D_Part^ part = gcnew STEP3D_Part();
    
    part->stepId = pw.stepId;
    part->name = Tools::toUnquotedString(pw.name);
    part->type = Tools::toString(pw.type);

    // TODO: add Placement

    part->representation_type = Tools::toString(pw.representation_type);
    
    return part;
}

STEP3D_PartRelation^ STEP3DAdapter::STEP3DFile::createRelationPart(const Relation_Wrapper& rw)
{
    STEP3D_PartRelation^ relation = gcnew STEP3D_PartRelation();
    
    relation->stepId = rw.stepId;
    relation->type = Tools::toString(rw.type);
    relation->id = Tools::toUnquotedString(rw.id);
    relation->name = Tools::toUnquotedString(rw.name);

    relation->relating_id = rw.relating_id;
    relation->related_id = rw.related_id;

#ifdef WITH_RELATION_PART_REFERENCES
    bool withRelating = false;
    bool withRelated = false;

    for each (auto part in m_parts)
    {
        if (!withRelating && (part->stepId == relation->relating_id))
        {
            relation->relating_part = part;
            withRelating = true;
        }

        if (!withRelated && (part->stepId == relation->related_id))
        {
            relation->related_part = part;
            withRelating = true;
        }

        if (withRelating && withRelated) break;
    }
#endif

    return relation;
}
