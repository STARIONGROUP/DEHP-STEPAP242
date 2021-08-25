// <copyright file="DstObjectBrowserViewModelFixturecs" company="Open Engineering S.A.">
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

using Moq;
using NUnit.Framework;

namespace DEHPSTEPAP242.Tests.Services
{
    using CDP4Common.EngineeringModelData;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPAP242.Services.FileStoreService;
    using DEHPSTEPAP242.Settings;
    using System.IO;

    [TestFixture]
    internal class FileStoreServiceFixture
    {
        private Mock<IUserPreferenceService<AppSettings>> preferenceService;
        private FileStoreService fstoreservice;
        private string fileStorePath;

        [SetUp]
        public void Setup()
        {
            string cwd = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);            
            fileStorePath = cwd + "/../../../../STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples";

            preferenceService = new Mock<IUserPreferenceService<AppSettings>>();
            preferenceService.Setup(x => x.Read());
            preferenceService.SetupGet(x => x.UserPreferenceSettings).Returns(new AppSettings { FileStoreCleanOnInit = true, FileStoreDirectoryName = fileStorePath });

            fstoreservice = new FileStoreService(preferenceService.Object);
        }

        [Test]
        public void TestInitialize()
        {
            Assert.DoesNotThrow(() => fstoreservice.InitializeStorage());
        }

        [Test]
        public void TestGetName()
        {
            FileRevision frev = new FileRevision();

            frev.Name = "testname.test";
            var fname = fstoreservice.GetName(frev);
            Assert.IsTrue(fname == "testname_rev0.test");
        }

        [Test]
        public void TestExists()
        {
            FileRevision frev = new FileRevision();

            frev.Name = "testname.test";
            Assert.IsFalse(fstoreservice.Exists(frev));
        }

        [Test]
        public void TestGetPath()
        {
            FileRevision frev = new FileRevision();

            frev.Name = "testname.test";
            var path = fstoreservice.GetPath(frev);
            Assert.IsTrue(path == Path.Combine(fileStorePath, "testname_rev0.test"));
        }
    }
}