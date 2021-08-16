// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StepComparisonTestFixture.cs" company="Open Engineering S.A.">
//    Copyright (c) 2021 Open Engineering S.A.
//
//    Author: Ivan Fontaine
//
//    Part of the code was based on the work performed by RHEA as result
//    of the collaboration in the context of "Digital Engineering Hub Pathfinder"
//    by Sam Gerené, Alex Vorobiev, Alexander van Delft and Nathanael Smiechowski.
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

using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
using DEHPSTEPAP242.Dialogs;
using DEHPSTEPAP242.DstController;
using DEHPSTEPAP242.ViewModel.Rows;
using Moq;
using NUnit.Framework;
using STEP3DAdapter;
using System.Collections.Generic;

namespace DEHPSTEPAP242.Tests.StepDiff
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class StepComparisonTestFixture
    {
        private DstCompareStepFilesViewModel dstCompareStepFilesViewModel;

        [SetUp]
        public void Setup()
        {
            Mock<IDstController> dstController = new Mock<IDstController>();
            Mock<IStatusBarControlViewModel> statusBar = new Mock<IStatusBarControlViewModel>();
            dstCompareStepFilesViewModel = new DstCompareStepFilesViewModel(dstController.Object, statusBar.Object);
        }

        /// <summary>
        /// In this this we check that when the content is different we keep both step data separated
        /// The most extreme test for the internal algorithm is simply when both step files
        /// </summary>
        [TestCase]
        public void TestDifferentFiles()
        {
            STEP3D_Part part1 = new STEP3D_Part
            {
                stepId = 1,
                type = "PD",
                name = "Spider",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation1 = new STEP3D_PartRelation
            {
                id = "Spider1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 211,
                type = "NUAO"
            };
            STEP3D_Part part2 = new STEP3D_Part
            {
                stepId = 1,// We use the same step id to check if the internal offset computation is working
                type = "PD",
                name = "Bolt",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation2 = new STEP3D_PartRelation
            {
                id = "Spider1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 211,
                type = "NUAO"
            };
            Step3DRowData stepData1 = new Step3DRowData(null, part1, relation1, "step_assembly");
            Step3DRowData stepData2 = new Step3DRowData(null, part2, relation2, "step_assembly");

            dstCompareStepFilesViewModel.SetHlrData(new List<Step3DRowData>() { stepData1 }, new List<Step3DRowData>() { stepData2 });
            dstCompareStepFilesViewModel.Process();
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR.Count == 2);
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR[0].PartOf == Step3DDiffRowViewModel.PartOfKind.FIRST);
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR[1].PartOf == Step3DDiffRowViewModel.PartOfKind.SECOND);
        }

        /// <summary>
        /// In this case we check two nodes that have the same name.
        /// </summary>
        [TestCase]
        public void TestSameFiles()
        {
            STEP3D_Part part1 = new STEP3D_Part
            {
                stepId = 1,
                type = "PD",
                name = "Spider",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation1 = new STEP3D_PartRelation
            {
                id = "Spider1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 211,
                type = "NUAO"
            };
            STEP3D_Part part2 = new STEP3D_Part
            {
                stepId = 1,// We use the same step id to check if the internal offset computation is working
                type = "PD",
                name = "Spider",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation2 = new STEP3D_PartRelation
            {
                id = "Spider1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 211,
                type = "NUAO"
            };
            Step3DRowData stepData1 = new Step3DRowData(null, part1, relation1, "step_assembly");
            Step3DRowData stepData2 = new Step3DRowData(null, part2, relation2, "step_assembly");

            dstCompareStepFilesViewModel.SetHlrData(new List<Step3DRowData>() { stepData1 }, new List<Step3DRowData>() { stepData2 });
            dstCompareStepFilesViewModel.Process();
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR.Count == 1);
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR[0].PartOf == Step3DDiffRowViewModel.PartOfKind.BOTH);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsEmpty(dstCompareStepFilesViewModel.Step3DHLR);
            Assert.IsNull(dstCompareStepFilesViewModel.FirstFileHeader);
            Assert.IsNull(dstCompareStepFilesViewModel.SecondFileHeader);
        }

        /// <summary>
        /// A merge whith one common node and two different nodes.
        /// </summary>
        [TestCase]
        public void TestMerge()
        {
            STEP3D_Part part1 = new STEP3D_Part
            {
                stepId = 1,
                type = "PD",
                name = "Spider",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation1 = new STEP3D_PartRelation
            {
                id = "Spider1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 211,
                type = "NUAO"
            };

            STEP3D_Part part12 = new STEP3D_Part
            {
                stepId = 2,
                type = "PD",
                name = "Botolo",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation12 = new STEP3D_PartRelation
            {
                id = "Botolo1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 124,
                type = "NUAO"
            };

            STEP3D_Part part2 = new STEP3D_Part
            {
                stepId = 1,// We use the same step id to check if the internal offset computation is working
                type = "PD",
                name = "Spider",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation2 = new STEP3D_PartRelation
            {
                id = "Spider1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 211,
                type = "NUAO"
            };

            STEP3D_Part part22 = new STEP3D_Part
            {
                stepId = 3,
                type = "PD",
                name = "Butulu",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation22 = new STEP3D_PartRelation
            {
                id = "Butulu1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 234,
                type = "NUAO"
            };

            Step3DRowData stepData1 = new Step3DRowData(null, part1, relation1, "step_assembly");
            Step3DRowData stepData2 = new Step3DRowData(null, part2, relation2, "step_assembly");

            Step3DRowData stepData3 = new Step3DRowData(null, part12, relation12, "step_assembly");
            Step3DRowData stepData4 = new Step3DRowData(null, part22, relation22, "step_assembly");

            dstCompareStepFilesViewModel.SetHlrData(new List<Step3DRowData>() { stepData1, stepData3 }, new List<Step3DRowData>() { stepData2, stepData4 });
            dstCompareStepFilesViewModel.Process();
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR.Count == 3);
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR[0].PartOf == Step3DDiffRowViewModel.PartOfKind.BOTH);
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR[1].PartOf == Step3DDiffRowViewModel.PartOfKind.FIRST);
            Assert.IsTrue(dstCompareStepFilesViewModel.Step3DHLR[2].PartOf == Step3DDiffRowViewModel.PartOfKind.SECOND);
        }
    }
}