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
		static void Main(string[] args)
		{
			var step3d = new STEP3DFile(args[0]);
			
			System.Console.WriteLine($"File name: { step3d.FileName }");

			if (step3d.HasFailed)
			{
				System.Console.WriteLine($"Error message: { step3d.ErrorMessage }");
			}

			System.Console.WriteLine("\n\nDATA ----------------------------------");

			var parts = step3d.Parts;

			foreach (var p in parts)
			{
				//Console.WriteLine($"");
				System.Console.WriteLine($"Part: #{p.id} {p.type} '{p.name}'");
			}

			var relations = step3d.Relations;

			foreach (var r in relations)
			{
				//Console.WriteLine($"");
				System.Console.WriteLine($"Relation: #{r.id} {r.type} '{r.name}' for #{r.relating_id} --> #{r.related_id}");
			}

#if WITH_RELATION_PART_REFERENCES
			if (parts[0] == relations[0].relating_part)
			{
				System.Console.WriteLine("Equal");
				System.Console.WriteLine($"Part: #{parts[0].id} {parts[0].type} <<{parts[0].name}>>");
				System.Console.WriteLine($"Part: #{relations[0].relating_part.id} {relations[0].relating_part.type} <<{relations[0].relating_part.name}>>");

				relations[0].relating_part.name = "New name for check if same object";

				System.Console.WriteLine($"Part: #{parts[0].id} {parts[0].type} <<{parts[0].name}>>");
				System.Console.WriteLine($"Part: #{relations[0].relating_part.id} {relations[0].relating_part.type} <<{relations[0].relating_part.name}>>");
			}
			else
			{
				System.Console.WriteLine("Differents");
			}
#endif
		}
	}
}
