/** --------------------------------------------------------------------------------------------------------------------
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
*/

using Autofac;
using CDP4Common.CommonData;
using CDP4Common.EngineeringModelData;
using CDP4Common.SiteDirectoryData;
using CDP4Common.Types;
using CDP4Dal;
using CDP4Dal.Operations;
using DEHPCommon;
using DEHPCommon.Enumerators;
using DEHPCommon.HubController.Interfaces;
using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;
using DEHPSTEPAP242.DstController;
using DEHPSTEPAP242.Services.DstHubService;
using DEHPSTEPAP242.ViewModel.Dialogs;
using DEHPSTEPAP242.ViewModel.Rows;
using Moq;
using NUnit.Framework;
using STEP3DAdapter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DEHPSTEPAP242.Tests.ViewModels
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class MappingConfigurationDialogViewModelFixture
    {
        private MappingConfigurationDialogViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<IHubController> hubController;
        private Mock<IDstHubService> hubService;

        private Iteration iteration;
        private Assembler assembler;

        //private Mock<IUserPreferenceService<AppSettings>> userPreferenceService;
        private string MyParts_path;

        private Mock<IStatusBarControlViewModel> statusBarViewModel;

        [SetUp]
        public void Setup()
        {
            dstController = new Mock<IDstController>();
            hubController = new Mock<IHubController>();
            hubService = new Mock<IDstHubService>();
            var containerBuilder = new ContainerBuilder();
            var uri = new Uri("http://t.e");
            this.assembler = new Assembler(uri);

            containerBuilder.RegisterType<HighLevelRepresentationBuilder>().As<IHighLevelRepresentationBuilder>();
            containerBuilder.RegisterType<HighLevelRepresentationBuilder>().As<IHighLevelRepresentationBuilder>();
            AppContainer.Container = containerBuilder.Build();
            string cwd = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            string examplesDir = cwd + "/../../../../../../STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples";
            examplesDir = System.IO.Path.GetFullPath(examplesDir);
            MyParts_path = System.IO.Path.Combine(examplesDir, "MyParts.step");

            this.iteration =
                new Iteration(Guid.NewGuid(), this.assembler.Cache, uri)
                {

                    Container = new EngineeringModel(Guid.NewGuid(), this.assembler.Cache, uri)
                    {
                        EngineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, uri)
                        {
                            RequiredRdl = { new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri) },
                            Container = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri)
                            {
                                Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, uri)
                            }
                        }
                    }


                };
            this.assembler.Cache.TryAdd(new CacheKey(this.iteration.Iid, null), new Lazy<Thing>(() => this.iteration));

            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);



            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<ExternalIdentifierMap>(), It.IsAny<Action<Iteration, ExternalIdentifierMap>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<IEnumerable<ElementDefinition>>(), It.IsAny<Action<Iteration, ElementDefinition>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.Delete(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(x => x.Write(It.IsAny<ThingTransaction>())).Returns(Task.CompletedTask);
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(new DomainOfExpertise());
            //this.hlrBuilder = new Mock<IHighLevelRepresentationBuilder>();

            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.statusBarViewModel.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));
            this.hubService = new Mock<IDstHubService>();

            const string oldCorrespondenceExternalId = "old";

            this.dstController.SetupGet(x => x.ExternalIdentifierMap).Returns(new ExternalIdentifierMap()
            {
                Correspondence =
                {
                    new IdCorrespondence() { ExternalId = oldCorrespondenceExternalId },
                    new IdCorrespondence() { ExternalId = "-1" },
                },
                Container = this.iteration
            });


            //            this.mappingEngine = new Mock<IMappingEngine>();

            //  userPreferenceService = new Mock<IUserPreferenceService<AppSettings>>();
            //            userPreferenceService.Setup(x => x.Read());
            //          userPreferenceService.SetupGet(x => x.UserPreferenceSettings).Returns(new AppSettings { FileStoreCleanOnInit = true, FileStoreDirectoryName = fileStorePath });

        }


        [Test]
        public void TestInitialize()
        {
            Assert.DoesNotThrow(() =>
            viewModel = new MappingConfigurationDialogViewModel(hubController.Object, dstController.Object, hubService.Object, statusBarViewModel.Object));

        }


        [Test]
        public void VerifyProperties()
        {
            viewModel = new MappingConfigurationDialogViewModel(hubController.Object, dstController.Object, hubService.Object, statusBarViewModel.Object);

            Assert.IsTrue(viewModel.SelectedThing == null);
            Assert.IsFalse(viewModel.IsBusy);
            Assert.IsEmpty(viewModel.AvailableOptions);
            Assert.IsEmpty(viewModel.AvailableElementDefinitions);
            Assert.IsEmpty(viewModel.AvailableElementUsages);
            Assert.IsEmpty(viewModel.AvailableActualFiniteStates);
        }

        private Step3DRowViewModel CreateViewModel()
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

            Step3DRowData stepData1 = new Step3DRowData(null, part1, relation1, "step_assembly");
            Step3DRowViewModel vm3d = new Step3DRowViewModel(stepData1);
            
            
            return vm3d;

        }

        [Test]
        public void TestSelectThing()
        {
            Assert.DoesNotThrow(() =>
               viewModel = new MappingConfigurationDialogViewModel(hubController.Object, dstController.Object, hubService.Object, statusBarViewModel.Object));

            
            Assert.DoesNotThrow(() => viewModel.SelectedThing = CreateViewModel());

        }

        public void TestSelectThingWithOption()
        {
            Assert.DoesNotThrow(() =>
               viewModel = new MappingConfigurationDialogViewModel(hubController.Object, dstController.Object, hubService.Object, statusBarViewModel.Object));

            Step3DRowViewModel vm3d = CreateViewModel();
            viewModel.SetPart(vm3d);
            
            var opts=new Option(Guid.NewGuid(), null, null);
            vm3d.SelectedOption = opts;
            
            var parameter = new Parameter()
            {
                ParameterType = new SimpleQuantityKind(),
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new [] {"654321"}),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };
            var elementDefinition = new ElementDefinition()
            {
                Parameter =
                {
                    parameter
                }
            };
            elementDefinition.Owner = new DomainOfExpertise();
            vm3d.SelectedElementDefinition = elementDefinition;
            
            Assert.DoesNotThrow(() => viewModel.SelectedThing = vm3d);
           

        }


        [Test]
        public void TestSetPart()
        {
            Assert.DoesNotThrow(() => viewModel.SetPart(CreateViewModel()));
        }

    }
}
