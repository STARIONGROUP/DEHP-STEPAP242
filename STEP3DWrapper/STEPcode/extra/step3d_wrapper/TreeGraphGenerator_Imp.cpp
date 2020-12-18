#pragma once

#include "TreeGraphGenerator_Imp.h"

#include <iostream>
#include <sstream>
#include <fstream>
#include <map>
#include <set>
using namespace std;


TreeGraphGenerator_Wrapper_Imp::TreeGraphGenerator_Wrapper_Imp()
{
    m_dot_relation_labeled = false;
};

TreeGraphGenerator_Wrapper_Imp::~TreeGraphGenerator_Wrapper_Imp()
{
    cout << "~TreeGraphGenerator_Wrapper_Imp" << endl;
}

bool TreeGraphGenerator_Wrapper_Imp::generate(IStep3D_Wrapper* wrapper, TreeGraphStyle mode)
{
    cout << "Generating Step3D graph..." << endl;

    m_filename = wrapper->getFilename();
    m_nodes = wrapper->getNodes();
    m_relations = wrapper->getRelations();
    m_dot_relation_labeled = mode == TreeGraphStyle::All_Graphs_LabelRelations; // activate labeling, for visual debugging

    if ((int)mode > 0)
    {
        buildDOT(mode);
    }
    else
    {
        buildDOT(TreeGraphStyle::Normal_DirGraph);
        buildDOT(TreeGraphStyle::RankdirLR_DirGraph);
        buildDOT(TreeGraphStyle::FolderSyle_DirGraph);
    }

    return true;
}

bool TreeGraphGenerator_Wrapper_Imp::buildDOT(TreeGraphStyle dottype)
{
    // Compose graph using the selected DOT type in the name
    ostringstream ss;
    ss << m_filename << "_" << (int)dottype << ".dot";

    string fname(ss.str());
    ofstream f(fname);

    if (dottype == TreeGraphStyle::Normal_DirGraph)
    {
        f << "digraph G {" << endl;
        f << "node [fontname=\"Courier New\", fontsize=10];" << endl;
        //f << "node [shape=ellipse];" << endl;
        f << "node [shape=box, style=\"filled, rounded\", fillcolor=\"#E5E5E5\"];" << endl;

        for (const auto& n : m_nodes)
        {
            f << "I" << n.stepId << " [label=\"" << n.type << "#" << n.stepId << " " << n.name << "\"];" << endl;
            //f << "I" << n.stepId << " [label=\"" << n.type << "#" << n.stepId << " (" << n.name << ")\"];" << endl;
        }

        for (const auto& r : m_relations)
        {
            if (m_dot_relation_labeled)
                f << "I" << r.relating_id << " -> I" << r.related_id << " [label=\"" << r.type << "#" << r.stepId << " " << r.id <<"\"];" << endl;
            else
                f << "I" << r.relating_id << " -> I" << r.related_id << ";" << endl;
        }

        // Convert all relating nodes in a BOX shape
        for (const auto& r : m_relations)
        {
            f << "I" << r.relating_id << " [shape=box, style=\"\"];" << endl;
        }
    }
    else if (dottype == TreeGraphStyle::RankdirLR_DirGraph)
    {
        f << "digraph G {" << endl;
        f << "rankdir=LR;" << endl;
        f << "fixedsize=true;" << endl;
        f << "node [style=\"rounded,filled\", width=0, height=0, shape=box, fillcolor=\"#E5E5E5\", concentrate=true];" << endl;
        f << endl;

        for (const auto& n : m_nodes)
        {
            f << "I" << n.stepId << " [label=\"" << n.type << "#" << n.stepId << " " << n.name << "\"];" << endl;
        }

        f << endl;

        for (const auto& r : m_relations)
        {
            if (m_dot_relation_labeled)
                f << "I" << r.relating_id << " -> I" << r.related_id << " [label=\"" << r.type << "#" << r.stepId << " " << r.id << "\"];" << endl;
            else
                f << "I" << r.relating_id << " -> I" << r.related_id << ";" << endl;
        }

        // Convert all relating nodes in a BOX shape
        for (const auto& r : m_relations)
        {
            f << "I" << r.relating_id << " [shape=box, style=\"\"];" << endl;
        }
    }
    else if (dottype == TreeGraphStyle::FolderSyle_DirGraph)
    {
        // construct list of relating and all its related items to construct the sequential associations
        std::map< int, std::list<int> > assemblies;
        std::set< int > relateds;

        for (const auto& r : m_relations)
        {
            assemblies[r.relating_id].push_back(r.related_id);
            relateds.insert(r.related_id);
        }

        f << "digraph tree" << endl;
        f << "{" << endl;

        f << "fixedsize=true;" << endl;
        f << "node [style=\"rounded,filled\", width=0, height=0, shape=box, fillcolor=\"#E5E5E5\"]" << endl;

        for (const auto& n : m_nodes)
        {
            if (relateds.find(n.stepId) == relateds.end())
            {
                f << "i_dir_" << n.stepId << " [label=\"" << n.type << "#" << n.stepId << " " << n.name << "\", width=2]" << endl;
            }
            else
            {
                f << "{rank=same" << endl;
                f << "  i_point_" << n.stepId << " [shape=point]" << endl;
                f << "  i_dir_" << n.stepId << " [label=\"" << n.type << "#" << n.stepId << " " << n.name << "\", width=2]" << endl;
                f << "}" << endl;
                f << "i_point_" << n.stepId << " -> " << "i_dir_" << n.stepId << endl;
            }
        }

        f << endl;
        f << endl;

        for (const auto& r : assemblies)
        {
            f << "i_dir_" << r.first;

            for (const auto& i : r.second)
            {
                f << " -> " << "i_point_" << i;
            }

            f << " [arrowhead=none]" << endl;
        }

        //for (const auto& r : m_relations)
        //{
        //    f << "i_dir_" << r.relating_id;
        //
        //    for (const auto& related_id : assemblies[r.relating_id])
        //    {
        //        f << " -> " << "i_point_" << related_id << " [arrowhead=none]" << endl;
        //
        //        assemblies[r.relating_id].push_back(r.related_id);
        //    }
        //    //f << "i_dir_" << r.relating_id << " -> " << "i_point_" << r.related_id << " [arrowhead=none]" << endl;
        //}
    }
    else
    {
        cerr << "DOT graph type not expected: " << (int)dottype << endl;
        return false;
    }

    f << "}" << endl;

    f.close();

    ostringstream oss;

    oss << "\"\"C:\\Program Files (x86)\\Graphviz2.38\\bin\\dot.exe\" -Tpng \"" << fname << "\" -o \"" << fname << ".png\"\"";

    cout << "RUN: " << oss.str() << endl;

    int retCode = system(oss.str().c_str());

    return retCode == 0;
}

void TreeGraphGenerator_Wrapper_Imp::Release()
{
    delete this;
}
