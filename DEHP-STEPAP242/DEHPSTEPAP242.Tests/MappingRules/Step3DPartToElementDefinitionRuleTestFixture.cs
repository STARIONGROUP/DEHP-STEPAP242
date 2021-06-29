// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Step3DPartToElementDefinitionRuleTestFixture.cs" company="Open Engineering S.A.">
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
using DEHPCommon.MappingEngine;
using DEHPCommon.Services.ExchangeHistory;
using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

using DEHPSTEPAP242.DstController;
using DEHPSTEPAP242.MappingRules;
using DEHPSTEPAP242.Services.DstHubService;
using DEHPSTEPAP242.ViewModel.Rows;
using Moq;
using NUnit.Framework;
using STEP3DAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using INavigationService = DEHPCommon.Services.NavigationService.INavigationService;

namespace DEHPSTEPAP242.Tests.MappingRules
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class Step3DPartToElementDefinitionRuleTestFixture
    {
        private const int ASSEMBLY_ID = 4;
        private const int ASSEMBLY_LABEL = 3;
        private const int ID = 1;
        private const int NAME = 0;
        private const int REPRESENTATION_TYPE = 2;
        private const int SOURCE = 5;
        private Assembler assembler;
        private DEHPSTEPAP242.DstController.DstController controller;
        private Mock<IDstHubService> dstHubService;
        private Mock<IExchangeHistoryService> exchangeHistoryService;        
        private Mock<IHubController> hubController;
        private Iteration iteration;
        private Mock<IMappingEngine> mappingEngine;
        private Mock<INavigationService> navigationService;
        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private readonly Mock<ISession> session = new Mock<ISession>();
        private DomainOfExpertise domain;


        [SetUp]
        public void Setup()
        {
            var uri = new Uri("http://t.e");
            this.assembler = new Assembler(uri);

            this.hubController = new Mock<IHubController>();

            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(new DomainOfExpertise());
            this.domain = new DomainOfExpertise(Guid.NewGuid(), assembler.Cache, uri);

            session.Setup(x => x.Assembler).Returns(this.assembler);
            session.Setup(x => x.DataSourceUri).Returns(uri.AbsoluteUri);

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

            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(domain);
            this.hubController.Setup(x => x.Session).Returns(session.Object);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            this.hubController.Setup(x => x.GetSiteDirectory()).Returns(new SiteDirectory());
            

            this.dstHubService = new Mock<IDstHubService>();
            this.dstHubService.Setup(x => x.GetReferenceDataLibrary()).Returns(new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri));
            this.dstHubService.Setup(x => x.IsSTEPParameterType(It.IsAny<ParameterType>())).Returns(true);
            this.mappingEngine = new Mock<IMappingEngine>();

            this.navigationService = new Mock<INavigationService>();

            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.statusBarViewModel.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.exchangeHistoryService = new Mock<IExchangeHistoryService>();
            this.exchangeHistoryService.Setup(x => x.Write()).Returns(Task.CompletedTask);

            this.controller = new DEHPSTEPAP242.DstController.DstController(this.hubController.Object,
                this.mappingEngine.Object, this.navigationService.Object, this.exchangeHistoryService.Object,
                this.dstHubService.Object, this.statusBarViewModel.Object);

            this.controller.SetExternalIdentifierMap(new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Container = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, null)
                {
                    Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, null)
                }
            });

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.controller).As<IDstController>();
            containerBuilder.RegisterInstance(this.dstHubService.Object).As<IDstHubService>();
            AppContainer.Container = containerBuilder.Build();
        }


        [TestCase]
        public void MappingRulesTestFixture_TransformOneSinglePart()
        {
            STEP3D_Part part = new STEP3D_Part
            {
                stepId = 1,
                type = "PD",
                name = "Spider1",
                representation_type = "Shape_Representation"
            };

            STEP3D_PartRelation relation = new STEP3D_PartRelation
            {
                id = "Spider1:1",
                related_id = 1,
                relating_id = 2,
                stepId = 211,
                type = "NUAO"
            };
            
            Step3DRowData stepData = new Step3DRowData(null,part, relation, "step_assembly");

            Step3DRowViewModel stepModel = new Step3DRowViewModel(stepData);

            
            stepModel.UpdateMappingStatus();
            List<Step3DRowViewModel> inputValues = new List<Step3DRowViewModel>();
            inputValues.Add(stepModel);
            Step3DPartToElementDefinitionRule translator = new Step3DPartToElementDefinitionRule();
            List<ElementBase> outputList;

            (_, outputList) = translator.Transform(inputValues);

            var elements = outputList.OfType<ElementDefinition>().ToList();

            var parameter = elements.Last().Parameter.First();

            var values = parameter.ValueSet.Last();
            // We check if the parameter type is correct
            Assert.IsTrue(parameter.ParameterType is CompoundParameterType);
            // We expect the user friendlyname to denote a step_geo parameter
            Assert.IsTrue(parameter.UserFriendlyName.Equals(".step_geo"));
            CompoundParameterType compound = (CompoundParameterType)parameter.ParameterType;

            var numberofvalues = compound.NumberOfValues;
            // We check the number of values
            Assert.IsTrue(numberofvalues == 6);
            // we check the values
           
            Assert.IsTrue(values.Computed[NAME].Equals("Spider1"));
            Assert.IsTrue(values.Computed[ID].Equals("1"));
            Assert.IsTrue(values.Computed[REPRESENTATION_TYPE].Equals("Shape_Representation"));
            Assert.IsTrue(values.Computed[ASSEMBLY_LABEL].Equals("Spider1:1"));
            Assert.IsTrue(values.Computed[ASSEMBLY_ID].Equals("1"));
            Assert.IsTrue(values.Computed[SOURCE].Equals(""));
        }       
    }
}