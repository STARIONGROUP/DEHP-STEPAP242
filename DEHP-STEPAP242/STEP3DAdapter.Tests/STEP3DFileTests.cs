using System;


namespace STEP3DAdapter.Tests
{
	using NUnit.Framework;
	using System.IO;

	[TestFixture]
	public class STEP3DFileTests
	{
		private string MyParts_path;
		private string NotStep3DFile_path;

		[OneTimeSetUp]
		public void ConfigureTest()
		{
			// Using files from the STEPcode library project:
			// Example current: D:\dev\DEHP\DEHP-STEPAP242\DEHP-STEPAP242\STEP3DAdapter.Tests\bin\Debug\
			// Example target:  D:\dev\DEHP\DEHP-STEPAP242\STEP3DWrapper\STEPcode\extra\step3d_wrapper_test\examples

			string cwd = System.AppContext.BaseDirectory;
			string examplesDir = cwd + "../../../../STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples";
			examplesDir = Path.GetFullPath(examplesDir);

			MyParts_path = Path.Combine(examplesDir, "MyParts.step");
			NotStep3DFile_path = Path.Combine(examplesDir, "NotStepFileFormat.step");
		}

		[TestCase]
		public void ShowTestSetup()
		{
			Console.WriteLine(MyParts_path);
			Console.WriteLine(NotStep3DFile_path);
		}

		[TestCase]
		public void CheckSTEPcodeVersion_PrefixMatch()
		{
			string expected = "git commit id: 0.8";
			string sc_version = STEP3DFile.STEPcodeVersion;

			Assert.IsTrue(sc_version.StartsWith(expected));			
		}

		[TestCase]
		public void LoadNotExistingFile_ShouldFail()
		{
			var step3d = new STEP3DFile("not-file-found.step");

			Assert.IsTrue(step3d.HasFailed);
			Assert.AreEqual("not-file-found.step", step3d.FileName);
			Assert.AreEqual("File does not exists: not-file-found.step", step3d.ErrorMessage);
		}

		[TestCase]
		public void LoadBadFormatFile_NotLoaded()
		{
			var step3d = new STEP3DFile(NotStep3DFile_path);

			Assert.IsTrue(step3d.HasFailed);
			Assert.AreEqual(NotStep3DFile_path, step3d.FileName);
			Assert.AreEqual("Error reading the STEP file content: SEVERITY_INPUT_ERROR", step3d.ErrorMessage);
		}

		[TestCase]
		public void LoadExistingFile_Loaded()
		{
			var step3d = new STEP3DFile(MyParts_path);

			Assert.IsFalse(step3d.HasFailed);
			Assert.AreEqual(MyParts_path, step3d.FileName);
		}

		[TestCase]
		public void CheckMyPartsFileContent_isOK()
		{
			var step3d = new STEP3DFile(MyParts_path);

			Assert.IsFalse(step3d.HasFailed);
			Assert.AreEqual(MyParts_path, step3d.FileName);

			// Check retrieved information

			var hdr = step3d.HeaderInfo;

			Assert.AreEqual("FreeCAD Model", hdr.file_description.description);
			Assert.AreEqual("2;1", hdr.file_description.implementation_level);
			Assert.AreEqual("D:/dev/DEHP/SharePoint/Project \nDocuments/XIPE_STEP_3D_Samples/MyParts.step", hdr.file_name.name);
			Assert.AreEqual("2020-09-01T18:50:05", hdr.file_name.time_stamp);
			Assert.AreEqual("Author", hdr.file_name.author);
			Assert.AreEqual("", hdr.file_name.organization);
			Assert.AreEqual("Open CASCADE STEP processor 7.2", hdr.file_name.preprocessor_version);
			Assert.AreEqual("FreeCAD", hdr.file_name.originating_system);
			Assert.AreEqual("Unknown", hdr.file_name.authorisation);
			Assert.AreEqual("AUTOMOTIVE_DESIGN { 1 0 10303 214 1 1 1 1 }", hdr.file_schema);

			//Console.WriteLine($"");

			var parts = step3d.Parts;
			foreach (var n in parts)
			{
				Console.WriteLine($"Part: #{n.id} {n.type} '{n.name}'");
			}

			var relations = step3d.Relations;
			foreach (var r in relations)
			{
				System.Console.WriteLine($"Relation: #{r.id} {r.type} '{r.name}' for #{r.relating_id} --> #{r.related_id}");
			}

			Assert.AreEqual(5, parts.Length);
			Assert.AreEqual(4, relations.Length);

			/*
			auto nodes = wrapper->getNodes();
			Assert::AreEqual((size_t)5, nodes.size());

			auto itNode = nodes.begin();
			Assert::AreEqual(5, itNode->id);
			Assert::AreEqual("'Part'", itNode->name.c_str());
			Assert::AreEqual("Shape_Representation", itNode->representation_type.c_str());

			itNode++;
			Assert::AreEqual(367, itNode->id);
			Assert::AreEqual("'Caja'", itNode->name.c_str());
			Assert::AreEqual("Advanced_Brep_Shape_Representation", itNode->representation_type.c_str());

			itNode++;
			Assert::AreEqual(380, itNode->id);
			Assert::AreEqual("'SubPart'", itNode->name.c_str());
			Assert::AreEqual("Shape_Representation", itNode->representation_type.c_str());

			itNode++;
			Assert::AreEqual(737, itNode->id);
			Assert::AreEqual("'Cube'", itNode->name.c_str());
			Assert::AreEqual("Advanced_Brep_Shape_Representation", itNode->representation_type.c_str());

			itNode++;
			Assert::AreEqual(854, itNode->id);
			Assert::AreEqual("'Cylinder'", itNode->name.c_str());
			Assert::AreEqual("Advanced_Brep_Shape_Representation", itNode->representation_type.c_str());

			auto relations = wrapper->getRelations();
			Assert::AreEqual((size_t)4, relations.size());

			auto itRel = relations.begin();
			Assert::AreEqual(376, itRel->id);
			Assert::AreEqual("'=>[0:1:1:1]'", itRel->name.c_str());
			Assert::AreEqual("NUAO", itRel->type.c_str());
			Assert::AreEqual(5, itRel->relating_id);
			Assert::AreEqual(367, itRel->related_id);

			itRel++;
			Assert::AreEqual(746, itRel->id);
			Assert::AreEqual("'=>[0:1:1:2]'", itRel->name.c_str());
			Assert::AreEqual("NUAO", itRel->type.c_str());
			Assert::AreEqual(380, itRel->relating_id);
			Assert::AreEqual(737, itRel->related_id);

			itRel++;
			Assert::AreEqual(863, itRel->id);
			Assert::AreEqual("'=>[0:1:1:3]'", itRel->name.c_str());
			Assert::AreEqual("NUAO", itRel->type.c_str());
			Assert::AreEqual(380, itRel->relating_id);
			Assert::AreEqual(854, itRel->related_id);

			itRel++;
			Assert::AreEqual(869, itRel->id);
			Assert::AreEqual("'=>[0:1:1:4]'", itRel->name.c_str());
			Assert::AreEqual("NUAO", itRel->type.c_str());
			Assert::AreEqual(5, itRel->relating_id);
			Assert::AreEqual(380, itRel->related_id);
			*/
		}
	}
}
