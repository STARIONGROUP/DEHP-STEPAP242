using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using STEP3DAdapter;


namespace STEP3DAdapter.Console
{
	class Program
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
