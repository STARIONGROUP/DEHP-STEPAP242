

namespace DEHPSTEPAP242.Tests.DstController
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.ViewModel.Rows;

    using DevExpress.Mvvm;
    using DevExpress.Mvvm.Native;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    using INavigationService = DEHPCommon.Services.NavigationService.INavigationService;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class DstControllerTestFixture
    {
        private DstController controller;
        private Mock<IHighLevelRepresentationBuilder> hlrBuilder;
        private Mock<IDstHubService> dstHubService;
        private Mock<IHubController> hubController;
        private Mock<IMappingEngine> mappingEngine;
        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private Mock<INavigationService> navigationService;

        private Iteration iteration;
        private Assembler assembler;


        private string cwd;
        private string examplesDir;
        private string MyParts_path;

        [SetUp]
        public void Setup()
        {
            cwd = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            examplesDir = cwd + "/../../../../../../STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples";
            examplesDir = System.IO.Path.GetFullPath(examplesDir);
            MyParts_path = System.IO.Path.Combine(examplesDir, "MyParts.step");


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

            this.assembler.Cache.TryAdd(new CacheKey(this.iteration.Iid, null),new Lazy<Thing>(() => this.iteration));

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


            this.hlrBuilder = new Mock<IHighLevelRepresentationBuilder>();

            this.dstHubService = new Mock<IDstHubService>();

            this.mappingEngine = new Mock<IMappingEngine>();

            this.navigationService = new Mock<INavigationService>();

            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.statusBarViewModel.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.controller = new DstController(this.hubController.Object, 
                this.mappingEngine.Object, this.navigationService.Object,
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
