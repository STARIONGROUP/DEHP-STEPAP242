// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Open Engineering S.A.">
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

namespace STEP3DAdapter.Console
{
    using STEP3DAdapter;
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage] // This is a developement tool.
    static class Program
    {
        static private void ShowSTEP3DInformation(String fname)
        {
            System.Console.WriteLine("STEP3D file information (from step3d_wrapper.dll)");
            System.Console.WriteLine("");

            var step3d = new STEP3DFile(fname);

            if (step3d.HasFailed)
            {
                System.Console.WriteLine($"Error message: { step3d.ErrorMessage }");
                return;
            }

            var hdr = step3d.HeaderInfo;
            var parts = step3d.Parts;
            var relations = step3d.Relations;

            System.Console.WriteLine($"\nFile name: { step3d.FileName }");

            System.Console.WriteLine("\nHEADER --------------------------------");
            System.Console.WriteLine("File_Description:");
            System.Console.WriteLine($"   description:          { hdr.file_description.description }");
            System.Console.WriteLine($"   implementation_level: { hdr.file_description.implementation_level }");
            System.Console.WriteLine("File_Name:");
            System.Console.WriteLine($"   name:                 { hdr.file_name.name }");
            System.Console.WriteLine($"   time_stamp:           { hdr.file_name.time_stamp }");
            System.Console.WriteLine($"   author:               { hdr.file_name.author }");
            System.Console.WriteLine($"   organization:         { hdr.file_name.organization }");
            System.Console.WriteLine($"   preprocessor_version: { hdr.file_name.preprocessor_version }");
            System.Console.WriteLine($"   originating_system:   { hdr.file_name.originating_system }");
            System.Console.WriteLine($"   authorisation:        { hdr.file_name.authorisation }");
            System.Console.WriteLine("File_Schema:");
            System.Console.WriteLine($"   schema:               { hdr.file_schema }");

            System.Console.WriteLine("\nDATA ----------------------------------");

            foreach (var p in parts)
            {
                System.Console.WriteLine($"Part: #{p.stepId} {p.type} '{p.name}'");
            }

            foreach (var r in relations)
            {

                System.Console.WriteLine($"Relation: #{r.id} {r.type} '{r.id},{r.name}' for #{r.relating_id} --> #{r.related_id}");
            }

#if WITH_RELATION_PART_REFERENCES
			if (parts[0] == relations[0].relating_part)
			{
				System.Console.WriteLine("Equal");
				System.Console.WriteLine($"Part: #{parts[0].stepId} {parts[0].type} <<{parts[0].name}>>");
				System.Console.WriteLine($"Part: #{relations[0].relating_part.id} {relations[0].relating_part.type} <<{relations[0].relating_part.name}>>");

				relations[0].relating_part.name = "New name for check if same object";

				System.Console.WriteLine($"Part: #{parts[0].stepId} {parts[0].type} <<{parts[0].name}>>");
				System.Console.WriteLine($"Part: #{relations[0].relating_part.id} {relations[0].relating_part.type} <<{relations[0].relating_part.name}>>");
			}
			else
			{
				System.Console.WriteLine("Differents");
			}
#endif
        }
        static void Main(string[] args)
        {
            foreach (var argument in args)
            {
                ShowSTEP3DInformation(argument);
            }
        }
    }
}
