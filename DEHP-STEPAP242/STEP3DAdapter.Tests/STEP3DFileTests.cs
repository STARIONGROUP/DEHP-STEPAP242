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


namespace STEP3DAdapter.Tests
{
    using NUnit.Framework;
    using System;
    using System.IO;

    [TestFixture]
    public class STEP3DFileTests
    {
        private string MyParts_path;
        private string NotStep3DFile_path;

#if Example_of_ArePartsEqual
		private static void AssertPartsEqual(in STEP3D_Part p1, in STEP3D_Part p2)
		{
			// Note: an operator == can be implemented in the struct
			bool eq = (p1.id == p2.id) && (p1.name == p2.name) && (p1.representation_type == p2.representation_type);
			return eq;
		}
#endif

        [OneTimeSetUp]
        public void ConfigureTest()
        {
            // Using files from the STEPcode library project:
            // Example current: D:\dev\DEHP\DEHP-STEPAP242\DEHP-STEPAP242\STEP3DAdapter.Tests\bin\Debug\
            // Example target:  D:\dev\DEHP\DEHP-STEPAP242\STEP3DWrapper\STEPcode\extra\step3d_wrapper_test\examples

            string cwd = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            string examplesDir = cwd + "/../../../../STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples";
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
            string expected = "0.9.1";
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
        public void CheckMyPartsFileContent_IsCorrect()
        {
            var step3d = new STEP3DFile(MyParts_path);

            Assert.IsFalse(step3d.HasFailed);

            var hdr = step3d.HeaderInfo;
            var parts = step3d.Parts;
            var relations = step3d.Relations;

            /* Check retrieved information */

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

#if DEBUG
            foreach (var n in parts)
            {
                Console.WriteLine($"Part: #{n.stepId} {n.type} '{n.name}'");
            }

            foreach (var r in relations)
            {
                System.Console.WriteLine($"Relation: #{r.id} {r.type} '{r.id},{r.name}' for #{r.relating_id} --> #{r.related_id}");
            }
#endif

            Assert.AreEqual(5, parts.Length);
            Assert.AreEqual(4, relations.Length);

            STEP3D_Part aPart;
            STEP3D_PartRelation aRelation;

            /* Parts */
            aPart = parts[0];
            Assert.AreEqual(5, aPart.stepId);
            Assert.AreEqual("Part", aPart.name);
            Assert.AreEqual("Shape_Representation", aPart.representation_type);

            aPart = parts[1];
            Assert.AreEqual(367, aPart.stepId);
            Assert.AreEqual("Caja", aPart.name);
            Assert.AreEqual("Advanced_Brep_Shape_Representation", aPart.representation_type);

            aPart = parts[2];
            Assert.AreEqual(380, aPart.stepId);
            Assert.AreEqual("SubPart", aPart.name);
            Assert.AreEqual("Shape_Representation", aPart.representation_type);

            aPart = parts[3];
            Assert.AreEqual(737, aPart.stepId);
            Assert.AreEqual("Cube", aPart.name);
            Assert.AreEqual("Advanced_Brep_Shape_Representation", aPart.representation_type);

            aPart = parts[4];
            Assert.AreEqual(854, aPart.stepId);
            Assert.AreEqual("Cylinder", aPart.name);
            Assert.AreEqual("Advanced_Brep_Shape_Representation", aPart.representation_type);

            /* Parts-Relation */
            aRelation = relations[0];
            Assert.AreEqual(376, aRelation.stepId);
            Assert.AreEqual("NUAO", aRelation.type);
            Assert.AreEqual("9", aRelation.id);
            Assert.AreEqual("=>[0:1:1:1]", aRelation.name);
            Assert.AreEqual(5, aRelation.relating_id);
            Assert.AreEqual(367, aRelation.related_id);

            aRelation = relations[1];
            Assert.AreEqual(746, aRelation.stepId);
            Assert.AreEqual("NUAO", aRelation.type);
            Assert.AreEqual("10", aRelation.id);
            Assert.AreEqual("=>[0:1:1:2]", aRelation.name);
            Assert.AreEqual(380, aRelation.relating_id);
            Assert.AreEqual(737, aRelation.related_id);

            aRelation = relations[2];
            Assert.AreEqual(863, aRelation.stepId);
            Assert.AreEqual("NUAO", aRelation.type);
            Assert.AreEqual("11", aRelation.id);
            Assert.AreEqual("=>[0:1:1:3]", aRelation.name);
            Assert.AreEqual(380, aRelation.relating_id);
            Assert.AreEqual(854, aRelation.related_id);

            aRelation = relations[3];
            Assert.AreEqual(869, aRelation.stepId);
            Assert.AreEqual("NUAO", aRelation.type);
            Assert.AreEqual("12", aRelation.id);
            Assert.AreEqual("=>[0:1:1:4]", aRelation.name);
            Assert.AreEqual(5, aRelation.relating_id);
            Assert.AreEqual(380, aRelation.related_id);
        }
    }
}
