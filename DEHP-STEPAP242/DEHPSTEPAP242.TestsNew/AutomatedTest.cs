using Autofac;
using CDP4Common.EngineeringModelData;
using CDP4Common.SiteDirectoryData;
using CDP4Dal.Operations;
using DEHPCommon;
using DEHPCommon.HubController.Interfaces;
using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
using DEHPSTEPAP242.ViewModel;
using NUnit.Framework;
using System;
using System.Threading;
using DEHPSTEPAP242.Services.DstHubService;
using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
using DEHPSTEPAP242.ViewModel.Dialogs;



namespace DEHPSTEPAP242.TestsNew
{
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserPreferenceHandler.Enums;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPAP242.Dialogs;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Services.FileStoreService;
    using DEHPSTEPAP242.Settings;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.ViewModel.NetChangePreview;
    using DEHPSTEPAP242.ViewModel.Rows;
    using ReactiveUI;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reflection;
    using System.Windows;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class AutomatedTests
    {
        private ContainerBuilder containerBuilder = null;
        private App application = null;

        // Credentials
        //private Uri uri = new Uri("http://localhost:5000");
        private Uri uri = new Uri("https://cdp4services-public.rheagroup.com");
        private string userName = "admin";
        private string password = "pass";


        private string SimpleCAD_Path = null;
        private string OtherCAD_Path = null;


        public void ApplicationOpen()
        {
            if(SimpleCAD_Path == null)
            {
                string cwd = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                string examplesDir = cwd + "/../../../../../../STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples";
                examplesDir = System.IO.Path.GetFullPath(examplesDir);
                SimpleCAD_Path = System.IO.Path.Combine(examplesDir, "SimpleCAD.step");
                OtherCAD_Path = System.IO.Path.Combine(examplesDir, "ModifiedSimpleCAD.step");


                //SimpleCAD_Path = "D:\\StepFiles\\SimpleCAD.step";     // To comment before commit
                //OtherCAD_Path = "D:\\StepFiles\\ModifiedCAD.step";    // To comment before commit 
            }



            if (containerBuilder == null)
            {
                containerBuilder = new ContainerBuilder();

                application = new App(containerBuilder);

                DstController dstController = (DstController)AppContainer.Container.Resolve<IDstController>();
                dstController.CodeCoverageState = true;
            }

            
            // For each test : rebluid a new container... and activate code coverage state in order to avoid some UI interaction 
            {
                containerBuilder = new ContainerBuilder();
                App.RegisterTypes(containerBuilder);
                App.RegisterViewModels(containerBuilder);

                AppContainer.BuildContainer(containerBuilder);
                
                DstController dstController = (DstController)AppContainer.Container.Resolve<IDstController>();
                dstController.CodeCoverageState = true;
            }

            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
        }

        public void RemoveModelFromHub(IHubController hubController, string modelName)
        {
            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();
           
            Console.WriteLine(hubController.IsSessionOpen);
            Console.WriteLine(hubController.Session.Name);

            SiteDirectory siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();

            // Delete the existing engineering model if it exists (support the case if there are several)            
            while (true)  
            {
                EngineeringModelSetup modelSetup2 = siteDirectory.Model.Find(x => x.UserFriendlyName == modelName);
                if (modelSetup2 is null)
                    break;

                Console.WriteLine("Deleting Model on HUB");

                SiteDirectory siteDirectoryCloned2 = siteDirectory.Clone(false);
                ThingTransaction transaction2 = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(siteDirectoryCloned2), siteDirectoryCloned2);

                EngineeringModelSetup testModelSetupCloned = modelSetup2.Clone(false);
                transaction2.Delete(testModelSetupCloned);

                hubController.Write(transaction2).Wait();

                hubController.Refresh();
            }
            hubController.Close();
        }

        
        public void ResetEngineeringModel(IHubController hubController, string modelName,bool withStepRef)
        {
            RemoveModelFromHub(hubController, modelName);   // Does nothing if the model does not exist

            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();
            Console.WriteLine(hubController.IsSessionOpen);
            Console.WriteLine(hubController.Session.Name);

            SiteDirectory siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();

            
            // Create the empty model
            ////////////////////////////
            SiteDirectory siteDirectoryCloned = siteDirectory.Clone(false);   // Try false
            ThingTransaction transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(siteDirectoryCloned), siteDirectoryCloned);


            ModelReferenceDataLibrary modelRDL = new ModelReferenceDataLibrary(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "EM1_RDL",
                ShortName = "EM1_RDL",
                RequiredRdl = siteDirectory.SiteReferenceDataLibrary[0]
            };
            transaction.CreateOrUpdate(modelRDL);

            EngineeringModelSetup modelSetup = new EngineeringModelSetup(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = modelName,
                ShortName = modelName,
            };
                        
            modelSetup.RequiredRdl.Add(modelRDL);

            EngineeringModel engineeringModel = new EngineeringModel(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                EngineeringModelSetup = modelSetup
            };
            modelSetup.EngineeringModelIid = engineeringModel.Iid;
                        
            transaction.CreateOrUpdate(modelSetup);

            siteDirectoryCloned.Model.Add(modelSetup);

            hubController.Write(transaction).Wait();
            
            Console.WriteLine("Model created");


            // Open the iteration


            EngineeringModelSetup modelSetup3 = siteDirectory.Model.Find(x => x.UserFriendlyName == modelName);
            DomainOfExpertise domExpertise = modelSetup3.ActiveDomain[0];

            Console.WriteLine(modelSetup3.UserFriendlyName);
            Console.WriteLine(domExpertise.UserFriendlyName);

            var model = new EngineeringModel(modelSetup3.EngineeringModelIid, hubController.Session.Assembler.Cache, uri);
            var itIid = modelSetup3.IterationSetup[0].IterationIid;
            var iteration = new Iteration(itIid, hubController.Session.Assembler.Cache, uri);
            model.Iteration.Add(iteration);
            hubController.GetIteration(iteration, domExpertise).Wait(); 

            iteration = hubController.OpenIteration;
                                  
            var iterationCloned = iteration.Clone(true);

            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(iterationCloned), iterationCloned);

            ElementDefinition edMission = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Mission",
                ShortName = "Mission",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edMission);
            iterationCloned.Element.Add(edMission);

            ElementDefinition edSatellite = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Satellite",
                ShortName = "Satellite",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edSatellite);
            iterationCloned.Element.Add(edSatellite);

            ElementDefinition edOBC = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "OBC",
                ShortName = "OBC",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edOBC);
            iterationCloned.Element.Add(edOBC);

            ElementDefinition edPCDU = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "PCDU",
                ShortName = "PCDU",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edPCDU);
            iterationCloned.Element.Add(edPCDU);

            ElementDefinition edRadiator = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Radiator",
                ShortName = "Radiator",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edRadiator);
            iterationCloned.Element.Add(edRadiator);

            hubController.Write(transaction).Wait();

            // Define EU under Satellite
            var edSatelliteCloned = edSatellite.Clone(false);
            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edSatelliteCloned), edSatelliteCloned);
            ElementUsage euOBC1 = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "OBC1",
                ShortName = "OBC1",
                Owner = domExpertise,
                ElementDefinition = edOBC
            };
            transaction.CreateOrUpdate(euOBC1);
            edSatelliteCloned.ContainedElement.Add(euOBC1);
            ElementUsage euOBC2 = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "OBC2",
                ShortName = "OBC2",
                Owner = domExpertise,
                ElementDefinition = edOBC
            };
            transaction.CreateOrUpdate(euOBC2);
            edSatelliteCloned.ContainedElement.Add(euOBC2);
            ElementUsage euPCDU = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "PCDU",
                ShortName = "PCDU",
                Owner = domExpertise,
                ElementDefinition = edPCDU
            };
            transaction.CreateOrUpdate(euPCDU);
            edSatelliteCloned.ContainedElement.Add(euPCDU);
            ElementUsage euRadiator = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Radiator",
                ShortName = "Radiator",
                Owner = domExpertise,
                ElementDefinition = edRadiator
            };
            transaction.CreateOrUpdate(euRadiator);
            edSatelliteCloned.ContainedElement.Add(euRadiator);
            hubController.Write(transaction).Wait();


            // Define EU under Mission
            var edMissionCloned = edMission.Clone(false);
            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edMissionCloned), edMissionCloned);
            ElementUsage euSatellite = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Satellite",
                ShortName = "Satellite",
                Owner = domExpertise,
                ElementDefinition = edSatellite
            };
            transaction.CreateOrUpdate(euSatellite);
            edMissionCloned.ContainedElement.Add(euSatellite);
            hubController.Write(transaction).Wait();



            // Define "Mode" PossibleFiniteStateList
            var iterationCloned2 = hubController.OpenIteration.Clone(false);
            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(iterationCloned2), iterationCloned2);
            PossibleFiniteStateList modeFSList = new PossibleFiniteStateList(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Mode",
                ShortName = "Mode",
                Owner = domExpertise
            };
            iterationCloned2.PossibleFiniteStateList.Add(modeFSList);

            PossibleFiniteState launchFS = new PossibleFiniteState(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Launch mode",
                ShortName = "Launch_mode",
            };
            modeFSList.PossibleState.Add(launchFS);
            transaction.CreateOrUpdate(launchFS);
            PossibleFiniteState nominalFS = new PossibleFiniteState(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Nominal mode",
                ShortName = "Nominal_mode",
            };
            modeFSList.PossibleState.Add(nominalFS);
            transaction.CreateOrUpdate(nominalFS);
            PossibleFiniteState safeFS = new PossibleFiniteState(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Safe mode",
                ShortName = "Safe_mode",
            };
            modeFSList.PossibleState.Add(safeFS);
            transaction.CreateOrUpdate(safeFS);

            transaction.CreateOrUpdate(modeFSList);

            hubController.Write(transaction).Wait();


            // Define the actualFS
            var iterationCloned3 = hubController.OpenIteration.Clone(false);
            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(iterationCloned3), iterationCloned3);
            ActualFiniteStateList actualFSList = new ActualFiniteStateList(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Owner = domExpertise
            };
            actualFSList.PossibleFiniteStateList.Add(modeFSList);
            iterationCloned3.ActualFiniteStateList.Add(actualFSList);
            transaction.CreateOrUpdate(actualFSList);
            hubController.Write(transaction).Wait();


            // mean consumed power
            {
                var model2 = hubController.OpenIteration.GetContainerOfType<EngineeringModel>();
                var modelSetup2 = model2.EngineeringModelSetup;
                var rdls = modelSetup2.RequiredRdl;
                var rdl = rdls.First();
                var parameters = rdl.QueryParameterTypesFromChainOfRdls();
                var scales = rdl.QueryMeasurementScalesFromChainOfRdls();

                var finiteStateList = hubController.OpenIteration.ActualFiniteStateList;
                int nbFinitesStates = finiteStateList.Count;
                var finiteStates = finiteStateList[0];

                ParameterType paramType = parameters.FirstOrDefault(x => x.Name == "mean consumed power" && !x.IsDeprecated);
                MeasurementScale scale = scales.FirstOrDefault(x => x.Name == "watt" && !x.IsDeprecated);

                // consumed mean power for PCDU ED
                {
                    var edPCDUCloned = edPCDU.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edPCDUCloned), edPCDUCloned);
                    Parameter newParam = new Parameter(Guid.NewGuid(), null, null)
                    {
                        ParameterType = paramType,
                        Owner = hubController.CurrentDomainOfExpertise,
                        Scale = scale, // finiteStates
                        StateDependence = null,
                    };
                    edPCDUCloned.Parameter.Add(newParam);
                    transaction.CreateOrUpdate(newParam);
                    hubController.Write(transaction).Wait();
                    edPCDU = hubController.OpenIteration.Element.FirstOrDefault(x => x.Name == "PCDU");
                    Parameter param = edPCDU.Parameter.FirstOrDefault(x => x.ParameterType.Name == "mean consumed power");
                    var paramCloned = param.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);
                    var newValue = paramCloned.QueryParameterBaseValueSet(null, null);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterValueSet)newValue).Manual[0] = "100";
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);
                    hubController.Write(transaction).Wait();
                }

                // consumed mean power for OBC ED
                {
                    var edOBCCloned = edOBC.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edOBCCloned), edOBCCloned);
                    Parameter newParam = new Parameter(Guid.NewGuid(), null, null)
                    {
                        ParameterType = paramType,
                        Owner = hubController.CurrentDomainOfExpertise,
                        Scale = scale, 
                        StateDependence = finiteStates
                    };
                    edOBCCloned.Parameter.Add(newParam);
                    transaction.CreateOrUpdate(newParam);
                    hubController.Write(transaction).Wait();


                    Console.WriteLine("FS1 -> " + finiteStates.ActualState[0].Name);
                    Console.WriteLine("FS2 -> " + finiteStates.ActualState[1].Name); 
                    Console.WriteLine("FS3 -> " + finiteStates.ActualState[2].Name);

                    edOBC = hubController.OpenIteration.Element.FirstOrDefault(x => x.Name == "OBC");
                    Parameter param = edOBC.Parameter.FirstOrDefault(x => x.ParameterType.Name == "mean consumed power");
                    var paramCloned = param.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);
                    var newValue = paramCloned.QueryParameterBaseValueSet(null,finiteStates.ActualState[0]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterValueSet)newValue).Manual[0] = "10";
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);
                    
                    newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[1]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterValueSet)newValue).Manual[0] = "30";
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);

                    newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[2]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterValueSet)newValue).Manual[0] = "50";
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);
                    
                    hubController.Write(transaction).Wait();
                }

                // Override mean consumed power on OBC2
                {
                    var euOBC2Cloned = euOBC2.Clone(false);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(euOBC2Cloned), euOBC2Cloned);
                    Parameter paramED = edOBC.Parameter.FirstOrDefault(x => x.ParameterType.Name == "mean consumed power");
                    ParameterOverride paramOver = new ParameterOverride(Guid.NewGuid(), null, null)
                    {
                        Parameter = paramED,
                        Owner = hubController.CurrentDomainOfExpertise
                    };
                    euOBC2Cloned.ParameterOverride.Add(paramOver);

                    transaction.CreateOrUpdate(paramOver);

                    hubController.Write(transaction).Wait();


                    euOBC2 = edOBC.ReferencingElementUsages().FirstOrDefault(x => x.Name == "OBC2");
                    ParameterOverride param = euOBC2.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == "mean consumed power");
                    var paramCloned = param.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);
                    var newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[0]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterOverrideValueSet)newValue).Manual[0] = "20";
                    transaction.CreateOrUpdate((ParameterOverrideValueSet)newValue);

                    newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[1]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterOverrideValueSet)newValue).Manual[0] = "40";
                    transaction.CreateOrUpdate((ParameterOverrideValueSet)newValue);

                    newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[2]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterOverrideValueSet)newValue).Manual[0] = "60";
                    transaction.CreateOrUpdate((ParameterOverrideValueSet)newValue);

                    hubController.Write(transaction).Wait();
                }
            }

            if (withStepRef)                    // Adding some "step geometry" with finite states
            {
                DstHubService dstHubService = (DstHubService)AppContainer.Container.Resolve<IDstHubService>();
                dstHubService.CheckHubDependencies().Wait();

                var model2 = hubController.OpenIteration.GetContainerOfType<EngineeringModel>();
                var modelSetup2 = model2.EngineeringModelSetup;
                var rdls = modelSetup2.RequiredRdl;
                var rdl = rdls.First();
                var parameters = rdl.ParameterType;

                var finiteStateList = hubController.OpenIteration.ActualFiniteStateList;
                int nbFinitesStates = finiteStateList.Count;
                var finiteStates = finiteStateList[0];

                // one transaction for each parameters
                var edOBCCloned = edOBC.Clone(true);
                transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edOBCCloned), edOBCCloned);
                ParameterType paramType = parameters.OfType<CompoundParameterType>().FirstOrDefault(x => x.Name == "step geometry" && !x.IsDeprecated);
                Parameter newParam = new Parameter(Guid.NewGuid(), null, null)
                {
                    ParameterType = paramType,
                    Owner = hubController.CurrentDomainOfExpertise,
                    StateDependence = finiteStates
                };
                edOBCCloned.Parameter.Add(newParam);
                transaction.CreateOrUpdate(newParam);
                hubController.Write(transaction).Wait();

                // Parameter override
                                
                var euOBC1Cloned = euOBC1.Clone(false); 
                transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(euOBC1Cloned), euOBC1Cloned);
                Parameter paramED = edOBCCloned.Parameter.FirstOrDefault(x => x.ParameterType.Name == "step geometry");
                ParameterOverride paramOver = new ParameterOverride(Guid.NewGuid(), null, null)
                {
                    Parameter = paramED,
                    Owner = hubController.CurrentDomainOfExpertise
                };
                euOBC1Cloned.ParameterOverride.Add(paramOver);

                transaction.CreateOrUpdate(paramOver);

                hubController.Write(transaction).Wait();
            }

            hubController.Close();

            return;
        }

        
        // The following method is not used anymore
        public void RestoreHubModelFromTemplateModel(IHubController hubController, string templateModelName, string testModelName)
        {
            Console.WriteLine("BEGIN RestoreHubModelFromTemplateModel");

            RemoveModelFromHub(hubController, testModelName);   // Does nothing if the model does not exist

            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();
            Console.WriteLine(hubController.IsSessionOpen);
            Console.WriteLine(hubController.Session.Name);

            SiteDirectory siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();
            EngineeringModelSetup templateModelSetup = siteDirectory.Model.Find(x => x.UserFriendlyName == templateModelName);

            DomainOfExpertise domExpertise = templateModelSetup.ActiveDomain[0];
            Console.WriteLine(templateModelSetup.UserFriendlyName);
            Console.WriteLine(domExpertise.UserFriendlyName);
            
            SiteDirectory siteDirectoryCloned = siteDirectory.Clone(false);  

            ThingTransaction transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(siteDirectoryCloned), siteDirectoryCloned);

            EngineeringModelSetup testModelSetup = new EngineeringModelSetup(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = testModelName,
                ShortName = testModelName,
                SourceEngineeringModelSetupIid = templateModelSetup.Iid,
            };

            EngineeringModel engineeringModel = new EngineeringModel(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                EngineeringModelSetup = testModelSetup
            };

            testModelSetup.EngineeringModelIid = engineeringModel.Iid;


            transaction.CreateOrUpdate(testModelSetup);

            siteDirectoryCloned.Model.Add(testModelSetup);

            hubController.Write(transaction).Wait();

            hubController.Close();

            Console.WriteLine("END RestoreHubModelFromTemplateModel");
            return;

        }


        public void OpenIterationOnHub(IHubController hubController, string modelName)
        {
            Console.WriteLine("Open iteration on hub");
            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();
            Console.WriteLine(hubController.Session.Name);

            var siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();


            EngineeringModelSetup modelSetup = siteDirectory.Model.Find(x => x.UserFriendlyName == modelName);

            DomainOfExpertise domExpertise = modelSetup.ActiveDomain[0];

            Console.WriteLine(modelSetup.UserFriendlyName);
            Console.WriteLine(domExpertise.UserFriendlyName);
            Console.WriteLine(modelSetup.IterationSetup[0].UserFriendlyName);


            var model = new EngineeringModel(modelSetup.EngineeringModelIid, hubController.Session.Assembler.Cache, uri);

            Console.WriteLine("Model Name " + model.UserFriendlyName);

            var itIid = modelSetup.IterationSetup[0].IterationIid;

            var iteration = new Iteration(itIid, hubController.Session.Assembler.Cache, uri);
            model.Iteration.Add(iteration);

            hubController.GetIteration(iteration, domExpertise).Wait(); // GetAwaiter().GetResult();

                        // What are the Option?
            Console.WriteLine("OPTIONS:");
            foreach (Option option in hubController.OpenIteration.Option)
            {
                Console.WriteLine("  Option " + option.Name);
            }
            if(hubController.OpenIteration.DefaultOption != null)
                Console.WriteLine("   Default Option " + hubController.OpenIteration.DefaultOption.Name);



            // What is the ED list of opened iteration?
            Console.WriteLine("ElementDefintions:");
            foreach (var elem in hubController.OpenIteration.Element)
            {
                Console.WriteLine("  ED " + elem.Name);

                var containedEUList = elem.ContainedElement;

                foreach(var entry in containedEUList)
                {
                    Console.WriteLine("    EU " + entry.Name);
                    foreach (var exclOpt in entry.ExcludeOption)
                    {
                        Console.WriteLine("      ExcludedOption " + exclOpt.Name);
                    }
                }
            }

            Console.WriteLine("END ElementDefintion of Open Model");
        }

        public void DeclareMapping(string name, string EDname,string EUname=null,string FSname=null)
        {
            Console.WriteLine("BEGIN Mapping");

            
            DstObjectBrowserViewModel dstObjectBrowserViewModel = (DstObjectBrowserViewModel)AppContainer.Container.Resolve<IDstObjectBrowserViewModel>();
            foreach (var entry in dstObjectBrowserViewModel.Step3DHLR)
            {
                var Name = entry.Name;
                var Path = entry.Description;
                var ElementName = entry.ElementName;
                var ID = entry.ID;
                var ParentID = entry.ParentID;
                var InstanceName = entry.InstanceName;
                var InstancePath = entry.InstancePath;
                var Description = entry.Description;
                var RelationId = entry.RelationId;

                Console.WriteLine("Name = " + Name);
            
            }
            Step3DRowViewModel rowVM = null;
            
            rowVM = dstObjectBrowserViewModel.Step3DHLR.Find(x => x.Name == name);

            if (rowVM is null)
            {
                Assert.Fail("rowVM not found");
                return;
            }

            Console.WriteLine("   rowVW " + name + "found!");

            dstObjectBrowserViewModel.SelectedPart = rowVM;
            dstObjectBrowserViewModel.PopulateContextMenu();   // For source code coverage
            dstObjectBrowserViewModel.MapCommand.ExecuteAsyncTask(null).Wait();


            MappingConfigurationDialogViewModel mappingConfigurationDialogViewModel = (MappingConfigurationDialogViewModel)AppContainer.Container.Resolve<IMappingConfigurationDialogViewModel>();

            mappingConfigurationDialogViewModel.SetPart(rowVM);    // SPA : mandatory if MappingConfigurationDialogViewModel is not a SingleInstance()

            var availableElementDefinitions = mappingConfigurationDialogViewModel.AvailableElementDefinitions;

            ElementDefinition elementDefinition = null;
            foreach (var elem in availableElementDefinitions)
            {
                if (elem.Name == EDname)
                {
                    elementDefinition = elem;
                    break;
                }
            }
            if (elementDefinition is null)
            {
                Console.WriteLine("   ED " + EDname + "not found!    Creating a new ED on the HUB");
                mappingConfigurationDialogViewModel.SelectedThing.SelectedElementDefinition = null;
                mappingConfigurationDialogViewModel.SelectedThing.NewElementDefinitionName = EDname;
            }
            else
            {
                Console.WriteLine("   ED " + EDname + " found!");
                mappingConfigurationDialogViewModel.SelectedThing.SelectedElementDefinition = elementDefinition;
                mappingConfigurationDialogViewModel.SelectedThing.NewElementDefinitionName = null;
            }


            //  Specific things 

            if (EUname != null)
            {
                var availableElementUsages = mappingConfigurationDialogViewModel.AvailableElementUsages;
                ElementUsage selectedEU = availableElementUsages.FirstOrDefault(x => x.Name == EUname);

                if (selectedEU != null)
                {
                        Console.WriteLine("    Mapping EU " + selectedEU.Name);
                        mappingConfigurationDialogViewModel.SelectedThing.SelectedElementUsages.Add(selectedEU);
                }
                else
                {
                        Assert.Fail("The EU " + selectedEU.Name + " does not exist");
                }
            }


            if (FSname != null)
            {
                var availableFiniteStates = mappingConfigurationDialogViewModel.AvailableActualFiniteStates;

                var selectedFS = availableFiniteStates.FirstOrDefault(x => x.Name == FSname);

                if (selectedFS != null)
                {
                    Console.WriteLine("    Mapping FS " + selectedFS.Name);
                    mappingConfigurationDialogViewModel.SelectedThing.SelectedActualFiniteState = selectedFS;
                }
                else
                {
                    Assert.Fail("The FS " + selectedFS.Name + " does not exist");
                }
            }

            mappingConfigurationDialogViewModel.ContinueCommand.ExecuteAsyncTask(null).Wait();
            
            Console.WriteLine("END Mapping");
        }



        [SetUp]
        public void Setup()
        {
        }


        //  this test is not used anymore ([Te
        public void TestHubDependencies()
        {
            ApplicationOpen();

            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            // Retore the used model on HUB from template model
            RestoreHubModelFromTemplateModel(hubController, "Template_STEPTAS_FiniteStates", "TestModel_STEPTAS_FiniteStates");
            OpenIterationOnHub(hubController, "TestModel_STEPTAS_FiniteStates");

            // Begin of test
            DstHubService dstHubService = (DstHubService)AppContainer.Container.Resolve<IDstHubService>();

            Console.WriteLine("Checking HUB dependencies");

            dstHubService.CheckHubDependencies().Wait();

            Console.WriteLine("END of Checking HUB dependencies");

            return;
        }

        
        [Test]
        public void EM1_NoConfig()
        {
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();
                        
            ResetEngineeringModel(hubController, "EM1_Test_STEPAP242", false);
            OpenIterationOnHub(hubController, "EM1_Test_STEPAP242");

            VerifyALL(1);
        }



        [Test]
        public void EM1_WithConfig()
        {
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            OpenIterationOnHub(hubController, "EM1_Test_STEPAP242");

            VerifyALL(1);
        }


        [Test]
        public void EM2_NoConfig()
        {
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            ResetEngineeringModel(hubController, "EM2_Test_STEPAP242", true);
            OpenIterationOnHub(hubController, "EM2_Test_STEPAP242");

            VerifyALL(2);
        }

        [Test]
        public void EM2_WithConfig()
        {
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            OpenIterationOnHub(hubController, "EM2_Test_STEPAP242"); 

            VerifyALL(2);
        }

        
        

        public void VerifyALL(int mappingNumber)
        {

            // Begin of test
            DstHubService dstHubService = (DstHubService)AppContainer.Container.Resolve<IDstHubService>();
            dstHubService.CheckHubDependencies().Wait();

            // call checkCheckHubDependencies() after constructor initialization  : IsSessionOpen
            HubDataSourceViewModel hubDataSourceViewModel = (HubDataSourceViewModel) AppContainer.Container.Resolve<IHubDataSourceViewModel>();


            UserPreferenceService<AppSettings> userPrefService = (UserPreferenceService<AppSettings>)AppContainer.Container.Resolve<IUserPreferenceService<AppSettings>>();
            userPrefService.Read();
            AppSettings settings = userPrefService.UserPreferenceSettings;
            Console.WriteLine(settings.FileStoreDirectoryName);
            Console.WriteLine(settings.FileStoreCleanOnInit);
            Console.WriteLine(settings.MappingUsedByFiles);
            foreach (var entry in settings.MappingUsedByFiles)
                Console.WriteLine("  Configuration: " + entry.Key + "  -->  " + entry.Value);


            // Opening the STEP-TAS file
            DstLoadFileViewModel dstLoadFileViewModel = (DstLoadFileViewModel)AppContainer.Container.Resolve<IDstLoadFileViewModel>();
            dstLoadFileViewModel.FilePath = SimpleCAD_Path;
            dstLoadFileViewModel.LoadFileCommand.ExecuteAsyncTask(null).Wait();
            
            // Default dehavior for Mapping configuration
            MappingConfigurationManagerDialogViewModel mappingConfigurationManagerDialogViewModel
              = (MappingConfigurationManagerDialogViewModel) AppContainer.Container.Resolve<IMappingConfigurationManagerDialogViewModel>();

            var existingExternalIdentifierMap = mappingConfigurationManagerDialogViewModel.AvailableExternalIdentifierMap;

            bool existingConfiguration = false;

            if (existingExternalIdentifierMap.Count > 0)
            {
                Console.WriteLine("USING an existing Mapping configuration");
                for(int i=0 ; i < existingExternalIdentifierMap.Count; i++)
                    Console.WriteLine("   " + i + " --> " + existingExternalIdentifierMap[i].Name);

                mappingConfigurationManagerDialogViewModel.CreateNewMappingConfigurationChecked = false;
                mappingConfigurationManagerDialogViewModel.SelectedExternalIdentifierMap = existingExternalIdentifierMap[0];

                existingConfiguration = true;
            }
            else
            {
                Console.WriteLine("Creating a NEW Mapping configuration");
                mappingConfigurationManagerDialogViewModel.CreateNewMappingConfigurationChecked = true;
                mappingConfigurationManagerDialogViewModel.NewExternalIdentifierMapName = "SimpleCAD Configuration";
            }

            mappingConfigurationManagerDialogViewModel.ApplyCommand.ExecuteAsyncTask(null).Wait();

            

            // Define the mappings and transfer them
            DstTransferControlViewModel dstTransferControlViewModel = (DstTransferControlViewModel)AppContainer.Container.Resolve<ITransferControlViewModel>();

            MappingViewModel mappingViewModel = (MappingViewModel)AppContainer.Container.Resolve<IMappingViewModel>();

            if (!existingConfiguration)
            {
                if (mappingNumber == 1)
                {
                    DeclareMapping("Box", "OBC");
                    DeclareMapping("Cylinder", "PCDU");
                    DeclareMapping("SubPart", "Radiator");
                }
                else
                {
                    DeclareMapping("Part", "OBC", null, "Launch mode");   // To define the mapping on the ED (without affecting the one defined on OBC1 just before
                    DeclareMapping("Box", "OBC", "OBC1", "Launch mode");
                    DeclareMapping("Cylinder", "PCDU");
                    DeclareMapping("SubPart", "Radiator");
                }
            }
            else
            {
                if (mappingNumber == 1)
                {
                    DeclareMapping("Cube", "OBC");
                    DeclareMapping("Part", "PCDU");
                    DeclareMapping("SubPart", "ELEMENT_CREATED_FROM_ADAPTER");
                }
                else
                {
                    DeclareMapping("Part", "OBC", "OBC1", "Launch mode");
                    DeclareMapping("Cube", "OBC", "OBC2", "Launch mode");   
                    DeclareMapping("Box", "Radiator");
                    DeclareMapping("SubPart", "ELEMENT_CREATED_FROM_ADAPTER");
                }
            }


            // We deselect and select the  mappings  :  --> At the end everything is selected!!!!
            HubNetChangePreviewViewModel hubNetChangePreviewViewModel = (HubNetChangePreviewViewModel)AppContainer.Container.Resolve<IHubNetChangePreviewViewModel>();
            hubNetChangePreviewViewModel.UpdateTree(false);

            hubNetChangePreviewViewModel.DeselectAllCommand.ExecuteAsyncTask(null).Wait();
            hubNetChangePreviewViewModel.SelectAllCommand.ExecuteAsyncTask(null).Wait();

            // We transfer the mappings that were defined... at the same times....
            dstTransferControlViewModel.TransferCommand.ExecuteAsyncTask(null).Wait();

            Console.WriteLine("Mapping transfer was done");
                       
            

            HubFileStoreBrowserViewModel hubFileStoreBrowserViewModel = (HubFileStoreBrowserViewModel) AppContainer.Container.Resolve<IHubFileStoreBrowserViewModel>();

            for(int i=0;i< hubFileStoreBrowserViewModel.HubFiles.Count;i++)
            {
                Console.WriteLine("FilePath   " + hubFileStoreBrowserViewModel.HubFiles[i].FilePath);
                Console.WriteLine("RevisonNumber   " + hubFileStoreBrowserViewModel.HubFiles[i].RevisionNumber);
                 
            }
           

            hubFileStoreBrowserViewModel.CurrentHubFile = hubFileStoreBrowserViewModel.HubFiles[0];

            hubFileStoreBrowserViewModel.CompareFileCommand.ExecuteAsyncTask(null).Wait(); 
                        
            Console.WriteLine("After CompareFileCommand (SAME FILES)");

                        
            
            FileStoreService fileStoreService = (FileStoreService) AppContainer.Container.Resolve<IFileStoreService>();
            hubFileStoreBrowserViewModel.LocalStepFilePath = fileStoreService.GetPath(hubFileStoreBrowserViewModel.CurrentFileRevision());
            
            
                        

            Console.WriteLine("Download STEP File to " + hubFileStoreBrowserViewModel.LocalStepFilePath);

            hubFileStoreBrowserViewModel.DownloadFileAsCommand.ExecuteAsyncTask(null).Wait();
            
            
            // Load another local STEP file 
            dstLoadFileViewModel.FilePath = OtherCAD_Path;
            dstLoadFileViewModel.LoadFileCommand.ExecuteAsyncTask(null).Wait();
            MappingConfigurationManagerDialogViewModel mappingConfigurationManagerDialogViewModel2
             = (MappingConfigurationManagerDialogViewModel)AppContainer.Container.Resolve<IMappingConfigurationManagerDialogViewModel>();
            mappingConfigurationManagerDialogViewModel2.CreateNewMappingConfigurationChecked = true;
            mappingConfigurationManagerDialogViewModel2.NewExternalIdentifierMapName = "ModifiedCAD Configuration";
            mappingConfigurationManagerDialogViewModel2.ApplyCommand.ExecuteAsyncTask(null).Wait();
                        
            hubFileStoreBrowserViewModel.CompareFileCommand.ExecuteAsyncTask(null).Wait(); 

            Console.WriteLine("After CompareFileCommand (OTHER FILES)");
                       


            /////////////////////////////////////////
            // Cover of IHubObjectBrowserViewModel //
            /////////////////////////////////////////
            HubObjectBrowserViewModel hubObjectBrowserViewModel = (HubObjectBrowserViewModel) AppContainer.Container.Resolve<IHubObjectBrowserViewModel>();
            hubObjectBrowserViewModel.BuildTrees();
            hubObjectBrowserViewModel.UpdateTree(true);
            hubObjectBrowserViewModel.Reload();

            Console.WriteLine("Nb entry in tree = " + hubObjectBrowserViewModel.Things.Count);
            foreach(var entity in hubObjectBrowserViewModel.Things)
            {
                //Console.WriteLine("entity.Name = " + entity.Name + "  " + entity);
                if (entity is ElementDefinitionsBrowserViewModel eds)
                {
                    foreach (var subentity in eds.ContainedRows)
                    {
                        hubObjectBrowserViewModel.SelectedThing = subentity;
                        hubObjectBrowserViewModel.SelectedThings.Clear();
                        hubObjectBrowserViewModel.SelectedThings.Add(subentity);
                        Console.WriteLine("subentity.type = " + subentity);
                        hubObjectBrowserViewModel.PopulateContextMenu();

                        foreach (var subsubentity in subentity.ContainedRows)
                        {
                            hubObjectBrowserViewModel.SelectedThing = subsubentity;
                            hubObjectBrowserViewModel.SelectedThings.Clear();
                            hubObjectBrowserViewModel.SelectedThings.Add(subsubentity);
                            Console.WriteLine(" subsubentity.type = " + subsubentity);
                            hubObjectBrowserViewModel.PopulateContextMenu();

                            foreach (var subsubsubentity in subsubentity.ContainedRows)
                            {
                                hubObjectBrowserViewModel.SelectedThing = subsubsubentity;
                                hubObjectBrowserViewModel.SelectedThings.Clear();
                                hubObjectBrowserViewModel.SelectedThings.Add(subsubsubentity);
                                //Console.WriteLine("   subsubsubentity.type = " + subsubsubentity);
                                hubObjectBrowserViewModel.PopulateContextMenu();
                            }
                        }
                    }                    
                }
            }

            Console.WriteLine("... END OF TEST!");
        }
       

        [Test]
        public void WindowMain_ConnectDisconnect()
        {
            ApplicationOpen();
            
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();
            OpenIterationOnHub(hubController, "EM2_Test_STEPAP242");

            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)AppContainer.Container.Resolve<IMainWindowViewModel>();
            

            /////////////////////////////
            // Disconnect from the hub //
            /////////////////////////////
            HubDataSourceViewModel hubDataSourceViewModel = (HubDataSourceViewModel)AppContainer.Container.Resolve<IHubDataSourceViewModel>();
            hubDataSourceViewModel.ConnectCommand.ExecuteAsyncTask(null).Wait();



            //////////////////////////////////////////////////////////////
            // Clean model files that were created on the HUB for test  //
            //////////////////////////////////////////////////////////////
            RemoveModelFromHub(hubController, "EM1_Test_STEPAP242");
            RemoveModelFromHub(hubController, "EM2_Test_STEPAP242");
        }


    }
}