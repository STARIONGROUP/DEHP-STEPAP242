// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
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

namespace DEHPSTEPAP242.Tests.DstController
{
    using CDP4Common;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using CDP4Dal;
    using CDP4Dal.Operations;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;
    using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.MappingRules;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.ViewModel.Rows;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using INavigationService = DEHPCommon.Services.NavigationService.INavigationService;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class DstControllerTestFixture
    {
        private DstController controller;
        //private Mock<IHighLevelRepresentationBuilder> hlrBuilder;
        private Mock<IDstHubService> dstHubService;
        private Mock<IHubController> hubController;
        private Mock<IMappingEngine> mappingEngine;
        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private Mock<INavigationService> navigationService;
        private Mock<IExchangeHistoryService> exchangeHistoryService;

        private Iteration iteration;
        private Assembler assembler;


        private string cwd;
        private string examplesDir;
        private string MyParts_path;
        private string NotStep3DFile_path;

        [SetUp]
        public void Setup()
        {
            cwd = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            examplesDir = cwd + "/../../../../../../STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples";
            examplesDir = System.IO.Path.GetFullPath(examplesDir);
            MyParts_path = System.IO.Path.Combine(examplesDir, "MyParts.step");
            NotStep3DFile_path = System.IO.Path.Combine(examplesDir, "NotStepFileFormat.step");

            this.hubController = new Mock<IHubController>();

            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(new DomainOfExpertise());

            var uri = new Uri("http://t.e");
            this.assembler = new Assembler(uri);

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


            //this.hlrBuilder = new Mock<IHighLevelRepresentationBuilder>();

            this.dstHubService = new Mock<IDstHubService>();

            this.mappingEngine = new Mock<IMappingEngine>();

            this.navigationService = new Mock<INavigationService>();

            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.statusBarViewModel.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.exchangeHistoryService = new Mock<IExchangeHistoryService>();
            this.exchangeHistoryService.Setup(x => x.Write()).Returns(Task.CompletedTask);

            this.controller = new DstController(this.hubController.Object,
                this.mappingEngine.Object, this.navigationService.Object, this.exchangeHistoryService.Object,
                this.dstHubService.Object, this.statusBarViewModel.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.Null(this.controller.Step3DFile);
            Assert.IsFalse(this.controller.IsFileOpen);
            Assert.AreEqual(MappingDirection.FromDstToHub, this.controller.MappingDirection);
            Assert.IsEmpty(this.controller.MapResult);
            Assert.IsEmpty(this.controller.IdCorrespondences);
            Assert.IsNull(this.controller.ExternalIdentifierMap);
            Assert.IsNotEmpty(this.controller.ThisToolName);
        }

        [Test]
        public void VerifyLoad()
        {
            Assert.DoesNotThrowAsync(async () => await this.controller.LoadAsync(MyParts_path));
            Assert.IsTrue(this.controller.IsFileOpen);
            Assert.IsNotNull(this.controller.Step3DFile);
            Assert.IsFalse(this.controller.IsLoading);
        }

        [Test]
        public void VerifyLoad_BadFormat()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await this.controller.LoadAsync(NotStep3DFile_path));
            Assert.IsFalse(this.controller.IsFileOpen);
            Assert.IsNull(this.controller.Step3DFile);
            Assert.IsFalse(this.controller.IsLoading);
        }

        [Test]
        public void VerifyCreateExternalIdentifierMap()
        {
            var newExternalIdentifierMap = this.controller.CreateExternalIdentifierMap("Name");
            this.controller.SetExternalIdentifierMap(newExternalIdentifierMap);
            Assert.AreEqual("Name", this.controller.ExternalIdentifierMap.Name);
            Assert.AreEqual("Name", this.controller.ExternalIdentifierMap.ExternalModelName);
            Assert.AreEqual(this.controller.ThisToolName, this.controller.ExternalIdentifierMap.ExternalToolName);
        }

        [Test]
        public void VerifyAddToExternalIdentifierMap()
        {
            this.controller.SetExternalIdentifierMap(this.controller.CreateExternalIdentifierMap("test"));

            var internalId = Guid.NewGuid();
            this.controller.AddToExternalIdentifierMap(internalId, string.Empty);
            Assert.IsNotEmpty(this.controller.IdCorrespondences);

            this.controller.AddToExternalIdentifierMap(internalId, string.Empty);
            this.controller.AddToExternalIdentifierMap(Guid.NewGuid(), string.Empty);
            this.controller.AddToExternalIdentifierMap(Guid.Empty, "ignored");
            Assert.AreEqual(2, this.controller.IdCorrespondences.Count);

            Assert.AreEqual(2, this.controller.UsedIdCorrespondences.Distinct().Count());
        }

        [Test]
        public void VerifyUpdateExternalIdentifierMap()
        {
            const string oldCorrespondenceExternalId = "old";

            this.controller.SetExternalIdentifierMap(new ExternalIdentifierMap()
            {
                Correspondence =
                {
                    new IdCorrespondence() { ExternalId = oldCorrespondenceExternalId },
                    new IdCorrespondence() { ExternalId = "-1" },
                },
                Container = this.iteration
            });

            this.controller.IdCorrespondences.AddRange(new[]
            {
                new IdCorrespondence() { ExternalId = "0"},
                new IdCorrespondence() { ExternalId = "1" },
                new IdCorrespondence() { ExternalId = "-1" }
            });

            Assert.DoesNotThrow(() => this.controller.MergeExternalIdentifierMap());

            Assert.AreEqual(5, this.controller.ExternalIdentifierMap.Correspondence.Count());
            Assert.IsNotNull(this.controller.ExternalIdentifierMap.Correspondence.SingleOrDefault(x => x.ExternalId == oldCorrespondenceExternalId));
            Assert.AreEqual(2, this.controller.ExternalIdentifierMap.Correspondence.Count(x => x.ExternalId == "-1"));

            Assert.IsEmpty(this.controller.IdCorrespondences);
            Assert.IsEmpty(this.controller.UsedIdCorrespondences);
            Assert.IsEmpty(this.controller.PreviousIdCorrespondences);
        }

        [Test]
        public void VerifyUpdateExternalIdentifierMap_RemoveUnused()
        {
            var correspondanceA = new IdCorrespondence() { ExternalId = "A" };
            var correspondanceB = new IdCorrespondence() { ExternalId = "B" };

            this.controller.SetExternalIdentifierMap(new ExternalIdentifierMap()
            {
                Correspondence =
                {
                    new IdCorrespondence() { ExternalId = "-1" },
                    correspondanceA,
                    correspondanceB,
                },
                Container = this.iteration
            });

            this.controller.PreviousIdCorrespondences.AddRange(new[]
            {
                correspondanceA,
                correspondanceB
            });

            this.controller.UsedIdCorrespondences.AddRange(new[]
            {
                correspondanceB
            });

            Assert.DoesNotThrow(() => this.controller.MergeExternalIdentifierMap());

            Assert.AreEqual(2, this.controller.ExternalIdentifierMap.Correspondence.Count());

            Assert.IsFalse(this.controller.ExternalIdentifierMap.Correspondence.Contains(correspondanceA));
            Assert.IsTrue(this.controller.ExternalIdentifierMap.Correspondence.Contains(correspondanceB));
            Assert.IsNull(this.controller.ExternalIdentifierMap.Correspondence.SingleOrDefault(x => x.ExternalId == "A"));
            Assert.IsNotNull(this.controller.ExternalIdentifierMap.Correspondence.SingleOrDefault(x => x.ExternalId == "B"));

            Assert.IsEmpty(this.controller.IdCorrespondences);
            Assert.IsEmpty(this.controller.UsedIdCorrespondences);
            Assert.IsEmpty(this.controller.PreviousIdCorrespondences);
        }

        [Test]
        public void VerifyAddPreviousIdCorrespondances()
        {
            var correspondances = new List<IdCorrespondence>()
            {
                new IdCorrespondence() { ExternalId = "A" },
                new IdCorrespondence() { ExternalId = "B" },
                new IdCorrespondence() { ExternalId = "C" },
            };

            this.controller.AddPreviousIdCorrespondances(correspondances);

            Assert.AreEqual(3, this.controller.PreviousIdCorrespondences.Count());

            this.controller.AddPreviousIdCorrespondances(new List<IdCorrespondence>()
            {
                new IdCorrespondence() { ExternalId = "new" }
            });

            Assert.AreEqual(1, this.controller.PreviousIdCorrespondences.Count());
        }

        [Test]
        public void VerifyMap()
        {
            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>()))
                .Returns((new List<ElementBase>(), new List<Step3DTargetSourceParameter>()));

            this.controller.SetExternalIdentifierMap(new ExternalIdentifierMap()
            {
                Container = this.iteration
            });

         //   Assert.DoesNotThrow(() => this.controller.Map(new Step3DRowViewModel(new STEP3DAdapter.STEP3D_Part(), new STEP3DAdapter.STEP3D_PartRelation())));

            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>())).Throws<InvalidOperationException>();
            Assert.Throws<NullReferenceException>(() => this.controller.Map(default(Step3DRowViewModel)));

            this.mappingEngine.Verify(x => x.Map(It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void VerifyTransferToHub()
        {
            this.navigationService.Setup(
                x => x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                It.IsAny<CreateLogEntryDialogViewModel>())).Returns(true);

            var aFile = new File();
            aFile.FileRevision.Add(new FileRevision(Guid.NewGuid(), null, null));

            this.dstHubService.Setup(x => x.FindFile(It.IsAny<string>())).Returns(aFile);

            this.controller.SetExternalIdentifierMap(new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Container = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, null)
                {
                    Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, null)
                }
            });

            this.controller.Load(MyParts_path);

            Assert.DoesNotThrowAsync(async () => await this.controller.Transfer());

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

            this.controller.MapResult.Add(elementDefinition);

            var parameterOverride = new ParameterOverride(Guid.NewGuid(), null, null)
            {
                Parameter = parameter,
                ValueSet =
                {
                    new ParameterOverrideValueSet()
                    {
                        Computed = new ValueArray<string>(new [] {"654321"}),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };

            this.controller.MapResult.Add(new ElementUsage()
            {
                ElementDefinition = elementDefinition,
                ParameterOverride =
                {
                    parameterOverride
                }
            });

            var map = new ExternalIdentifierMap();

            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out map));

            this.hubController.Setup(x =>
                x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out parameter));

            this.hubController.Setup(x =>
                x.GetThingById(parameterOverride.Iid, It.IsAny<Iteration>(), out parameterOverride));

            Assert.DoesNotThrowAsync(async () => await this.controller.Transfer());

            Assert.IsEmpty(this.controller.MapResult);

            this.controller.MapResult.Add(new ElementUsage());

            this.navigationService.Setup(
                x => x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                    It.IsAny<CreateLogEntryDialogViewModel>())).Returns(false);

            Assert.DoesNotThrowAsync(async () => await this.controller.Transfer());

            this.navigationService.Setup(
                x => x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                    It.IsAny<CreateLogEntryDialogViewModel>())).Returns(default(bool?));

            Assert.DoesNotThrowAsync(async () => await this.controller.Transfer());

            this.navigationService.Setup(
                x => x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                    It.IsAny<CreateLogEntryDialogViewModel>())).Returns(true);

            Assert.DoesNotThrowAsync(async () => await this.controller.Transfer());

            Assert.IsEmpty(this.controller.MapResult);

            Assert.DoesNotThrowAsync(async () => await this.controller.Transfer());

            this.navigationService.Verify(
                x =>
                    x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                        It.IsAny<CreateLogEntryDialogViewModel>())
                , Times.Exactly(4));

            this.hubController.Verify(
                x => x.Write(It.IsAny<ThingTransaction>()), Times.Exactly(4));

            this.hubController.Verify(
                x => x.Refresh(), Times.Exactly(2));

            this.exchangeHistoryService.Verify(x =>
                x.Append(It.IsAny<Thing>(), It.IsAny<ChangeKind>()), Times.Exactly(3));

            this.exchangeHistoryService.Verify(x =>
                x.Append(It.IsAny<ParameterValueSetBase>(), It.IsAny<IValueSet>()), Times.Exactly(2));
        }

        [Test]
        public void VerifyUpdateValueSets()
        {
            var element = new ElementDefinition();

            var parameter = new Parameter()
            {
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(new [] {"nok"}),
                        ValueSwitch = ParameterSwitchKind.MANUAL
                    }
                }
            };

            var elementUsage = new ElementUsage()
            {
                ElementDefinition = element
            };

            var parameterOverride = new ParameterOverride()
            {
                Parameter = parameter,
                ValueSet =
                {
                    new ParameterOverrideValueSet()
                    {
                        Reference = new ValueArray<string>(new[] { "nokeither" }),
                        ValueSwitch = ParameterSwitchKind.REFERENCE
                    }
                }
            };

            element.Parameter.Add(parameter);
            elementUsage.ParameterOverride.Add(parameterOverride);
            element.ContainedElement.Add(elementUsage);

            this.hubController.Setup(x => x.GetThingById(
                It.IsAny<Guid>(), It.IsAny<Iteration>(), out parameter)).Returns(true);

            this.hubController.Setup(x => x.GetThingById(
                It.IsAny<Guid>(), It.IsAny<Iteration>(), out parameterOverride)).Returns(true);

            this.controller.MapResult.Add(element);

            Assert.DoesNotThrowAsync(async () => await this.controller.UpdateParametersValueSets());

            this.hubController.Verify(x =>
                x.Write(It.IsAny<ThingTransaction>()), Times.Once);
        }
    }
}
