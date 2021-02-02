﻿
namespace DEHPSTEPAP242.Services.DstHubService
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Reactive.Linq;
    using System.Linq;
    using System.IO;

    using NLog;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    
    using DEHPCommon.HubController.Interfaces;

    using File = CDP4Common.EngineeringModelData.File;
    using System.Diagnostics;

    class DstHubService : IDstHubService
    {
        private static readonly string RDL_NAME = "Generic ECSS-E-TM-10-25 Reference Data Library";
        private static readonly string RDL_SHORT_NAME = "Generic_RDL";

        private static readonly string APPLICATION_STEP_NAME = "application/step";
        private static readonly string[] APPLICATION_STEP_EXTENSIONS = { "step", "stp" };

        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="DEHPCommon.IHubController"/> instance
        /// </summary>
        private IHubController hubController;

        public DstHubService(IHubController hubController)
        {
            this.hubController = hubController;
        }

        public async Task CheckHubDependencies()
        {
            if (this.hubController.OpenIteration is null) return;

            await CheckFileTypes();
            await CheckParameterTypes();
        }

        /// <summary>
        /// Finds the DST <see cref="CDP4Common.EngineeringModelData.File"/> in the Hub
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.File"/> or null if does not exist</returns>
        public File FindFile(string filePath)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);

            var currentDomainOfExpertise = hubController.CurrentDomainOfExpertise;
            var dfStore = hubController.OpenIteration.DomainFileStore.FirstOrDefault(d => d.Owner == currentDomainOfExpertise);

            var file = dfStore.File.FirstOrDefault(x => this.IsSTEPFileType(x.CurrentFileRevision) && x.CurrentFileRevision.Name == name);

            //var file = from x in dfStore.File
            //         where this.IsSTEPFileType(x.CurrentFileRevision) && x.CurrentFileRevision.Name == name
            //         select x;

            //foreach (var file in dfStore.File)
            //{
            //    var cfrev = file.CurrentFileRevision;
            //
            //    if (this.IsSTEPFileType(cfrev) && cfrev.Name == name)
            //    {
            //        return file;
            //    }
            //}

            return file;
        }

        /// <summary>
        /// First compatible STEP <see cref="FileType"/> of a <see cref="FileRevision"/>
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/></param>
        /// <returns>First compatible FileType or null if not</returns>
        public FileType FirstSTEPFileType(FileRevision fileRevision)
        {
            var fileTypeList = fileRevision.FileType;

            //logger.Debug($"FileType Extension Count: {fileTypeList.Count}");

            var fileType = fileTypeList.FirstOrDefault(t => (t.Name == APPLICATION_STEP_NAME));

            //if (fileType != null)
            //{
            //	logger.Debug($"  fileType Extension: {fileType.Name} {fileType.ShortName} {fileType.Extension}");
            //}

            return fileType;
        }

        /// <summary>
        /// Finds all the revisions for DST files
        /// </summary>
        /// <returns>The <see cref="List{FileRevision}"/> for only current file revision</returns>
        public List<FileRevision> GetFileRevisions()
        {
            var revisions = new List<FileRevision>();

            var currentDomainOfExpertise = hubController.CurrentDomainOfExpertise;
            logger.Debug($"Domain of Expertise: { currentDomainOfExpertise.Name }");

            var dfStore = hubController.OpenIteration.DomainFileStore.FirstOrDefault(d => d.Owner == currentDomainOfExpertise);
            logger.Debug($"DomainFileStore: {dfStore.Name} (Rev: {dfStore.RevisionNumber})");


            logger.Debug($"Files Count: {dfStore.File.Count}");
            foreach (var f in dfStore.File)
            {
                var cfrev = f.CurrentFileRevision;

                if (this.IsSTEPFileType(cfrev))
                {
                    revisions.Add(cfrev);
                }    
            }

            return revisions;
        }

        /// <summary>
        /// Checks if it is a STEP file type
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>True if is a STEP file</returns>
        public bool IsSTEPFileType(FileRevision fileRevision)
        {
            var fileType = this.FirstSTEPFileType(fileRevision);

            return (fileType is null) == false;
        }

        /// <summary>
        /// Gets the <see cref="ReferenceDataLibrary"/> where to add DST content
        /// </summary>
        /// <returns>A <see cref="ReferenceDataLibrary"/></returns>
        public ReferenceDataLibrary GetReferenceDataLibrary()
        {
            // Different RDL could exist in the server:
            // RDL: RDL specific to CDF_generic_template --> contains 0 FileTypes
            // RDL: Generic ECSS-E-TM-10-25 Reference Data Library --> contains 28 FileTypes

            // Search From Model
            // iteration --> contained in EM --> having a EM Setup with 1 RequiredRDL
            var model = this.hubController.OpenIteration.GetContainerOfType<EngineeringModel>();
            var modelSetup = model.EngineeringModelSetup;
            var rdls = modelSetup.RequiredRdl;

            var rdl = rdls.First();
            
            //// Search From SiteDirectory
            //var site = hubController.GetSiteDirectory();
            //var rdl = site.SiteReferenceDataLibrary.FirstOrDefault(r => r.ShortName == RDL_SHORT_NAME);
            //
            //if (rdl is null)
            //{
            //    logger.Error($"Unexpected ReferenceDataLibrary not found when looking for '{RDL_SHORT_NAME}'");
            //}

            return rdl;
        }

        /// <summary>
        /// Check that STEP <see cref="FileType"/> exists in the RDL
        /// 
        /// Target RDL: Generic_RDL
        /// 
        /// Two <see cref="FileType"/> are used:
        /// - application/step for .step extension
        /// - application/step for .stp extension
        /// 
        /// Adds missing <see cref="FileType"/>
        /// </summary>
        private async Task CheckFileTypes()
        {
            var rdl = GetReferenceDataLibrary();

            var missingExtensions = new List<string>();

            // Verify that any STEP well known extension is checked
            foreach (var ext in APPLICATION_STEP_EXTENSIONS)
            {
                if (rdl.FileType.Any(t => t.Extension == ext) == false)
                {
                    missingExtensions.Add(ext);
                }
            }

            if (missingExtensions.Count > 0)
            {
                var thingsToWrite = new List<FileType>();

                foreach (var extension in missingExtensions)
                {
                    var fileType = new FileType(Guid.NewGuid(), null, null)
                    {
                        Name = APPLICATION_STEP_NAME,
                        ShortName = APPLICATION_STEP_NAME,
                        Extension = extension,
                        Container = rdl
                    };

                    logger.Debug($"Adding missing STEP FileType {APPLICATION_STEP_NAME} for .{extension}");

                    thingsToWrite.Add(fileType);
                }

                await hubController.CreateOrUpdate<ReferenceDataLibrary, FileType>(thingsToWrite, (r, t) => r.FileType.Add(t));
            }
        }

        /// <summary>
        /// Checks that STEP 3D parameters exists
        /// 
        /// Things:
        /// - MeasurementScale: Type=OrdinalScale, Name=step id, ShortName=-, Unit=1, NumberSet=NATURAL_NUMBER_SET, MinPermsibleValue=0, MinInclusive=true (indicate not known value)
        /// 
        /// - ParameterType: Type=SimpleQuantityKind, Name=step id, ShortName=step_id, Symbol=#, DefaultScale=step id
        /// - ParameterType: Type=TextParameterType, Name=step label, ShortName=step_label, Symbol=-
        /// - ParameterType: Type=TextParameterType, Name=step file reference, ShortName=step_file_reference, Symbol=-
        /// 
        /// - ParameterType: Type=CompoundParameterType, Name=step 3d geometry, ShortName=step_3d_geom, Symbol=-
        ///      component1: Name=name,           Type=step label
        ///      component2: Name=id,             Type=step id
        ///      component3: Name=rep_type,       Type=step label
        ///      component4: Name=assembly_label, Type=step label
        ///      component5: Name=assembly_id,    Type=step id
        ///      component6: Name=source,         Type=step file reference
        /// </summary>
        private async Task CheckParameterTypes()
        {
            // Note 1: MeasurementScale represents the VIM concept of "quantity-value scale" 
            // that is defined as "ordered set of quantity values of quantities of a given 
            // kind of quantity used in ranking, according to magnitude, quantities of that kind".
            //
            // Note 2: A MeasurementScale defines how to interpret the numerical value of a quantity 
            // or parameter. In this data model a distinction is made between a measurement scale 
            // and a measurement unit.A measurement unit is a reference quantity that defines 
            // how to interpret an interval of one on a measurement scale. A measurement scale 
            // defines in addition the kind of scale, and where necessary more characteristics 
            // to provide all information needed for mapping quantity values between different scales, 
            // as specified in the specializations of this class.

            var rdl = this.GetReferenceDataLibrary();

            var units = rdl.Unit;
            var scales = rdl.Scale;
            var parameters = rdl.ParameterType;

            const string STEP_ID_UNIT_NAME = "step id";
            const string STEP_ID_NAME = "step id";
            const string STEP_LABEL_NAME = "step label";
            const string STEP_FILE_REF_NAME = "step file reference";
            const string STEP_GEOMETRY_NAME = "step geometry";

            MeasurementUnit oneUnit = units.OfType<SimpleUnit>().FirstOrDefault(u => u.ShortName == "1" && u is SimpleUnit);
            MeasurementScale stepIdScale = scales.OfType<OrdinalScale>().FirstOrDefault(x => x.Name == STEP_ID_NAME && !x.IsDeprecated);
            ParameterType stepIdParameter = parameters.OfType<SimpleQuantityKind>().FirstOrDefault(x => x.Name == STEP_ID_NAME && !x.IsDeprecated);
            ParameterType stepLabelParameter = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == STEP_LABEL_NAME && !x.IsDeprecated);
            ParameterType stepFileRefParameter = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == STEP_FILE_REF_NAME && !x.IsDeprecated);
            CompoundParameterType step3DGeometryParameter = parameters.OfType<CompoundParameterType>().FirstOrDefault(x => x.Name == STEP_GEOMETRY_NAME && !x.IsDeprecated);

            var rldClone = rdl.Clone(false);

            if (oneUnit is null)
            {
                oneUnit = new SimpleUnit(Guid.NewGuid(), null, null)
                {
                    Name = STEP_ID_UNIT_NAME,
                    ShortName = "1",
                    Container = rldClone
                };

                logger.Info($"Adding Unit: {oneUnit.Name} [{oneUnit.ShortName}]");

                await hubController.CreateOrUpdate<ReferenceDataLibrary, MeasurementUnit>(
                    oneUnit, (r, s) => r.Unit.Add(s));
            }

            if (stepIdScale is null)
            {
                stepIdScale = new OrdinalScale(Guid.NewGuid(), null, null)
                {
                    Name = STEP_ID_NAME,
                    ShortName = "-",
                    Unit = units.FirstOrDefault(u => u.ShortName == "1" && u is SimpleUnit),
                    NumberSet = NumberSetKind.NATURAL_NUMBER_SET,
                    MinimumPermissibleValue = "0",
                    IsMinimumInclusive = true, // 0 indicates not known value
                    Container = rldClone
                };

                logger.Info($"Adding Scale: {stepIdScale.Name} [{stepIdScale.ShortName}] Unit={stepIdScale.Unit.Name}");

                await hubController.CreateOrUpdate<ReferenceDataLibrary, MeasurementScale>(
                    stepIdScale, (r, s) => r.Scale.Add(s));
            }

            if (stepIdParameter is null)
            {
                stepIdParameter = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                {
                    Name = STEP_ID_NAME,
                    ShortName = "step_id",
                    Symbol = "#",
                    DefaultScale = stepIdScale,
                    PossibleScale = new List<MeasurementScale> { stepIdScale },
                    Container = rldClone
                };

                logger.Info($"Adding Parameter: {stepIdParameter.Name} [{stepIdParameter.ShortName}]");

                await hubController.CreateOrUpdate<ReferenceDataLibrary, ParameterType>(
                    stepIdParameter, (r, p) => r.ParameterType.Add(p));
            }

            if (stepLabelParameter is null)
            {
                stepLabelParameter = new TextParameterType(Guid.NewGuid(), null, null)
                {
                    Name = STEP_LABEL_NAME,
                    ShortName = "step_label",
                    Symbol = "-",
                    Container = rldClone
                };

                logger.Info($"Adding Parameter: {stepLabelParameter.Name} [{stepLabelParameter.ShortName}]");

                await hubController.CreateOrUpdate<ReferenceDataLibrary, ParameterType>(
                    stepLabelParameter, (r, p) => r.ParameterType.Add(p));
            }

            if (stepFileRefParameter is null)
            {
                stepFileRefParameter = new TextParameterType(Guid.NewGuid(), null, null)
                {
                    Name = STEP_FILE_REF_NAME,
                    ShortName = "step_file_reference",
                    Symbol = "-",
                    Container = rldClone
                };

                logger.Info($"Adding Parameter: {stepFileRefParameter.Name} [{stepFileRefParameter.ShortName}]");

                await hubController.CreateOrUpdate<ReferenceDataLibrary, ParameterType>(
                    stepFileRefParameter, (r, p) => r.ParameterType.Add(p));
            }

            // Once all sub-parameters exist, compound parameter can be created
            if (step3DGeometryParameter is null)
            {
                step3DGeometryParameter = new CompoundParameterType(Guid.NewGuid(), null, null)
                {
                    Name = STEP_GEOMETRY_NAME,
                    ShortName = "step_geo",
                    Symbol = "-",
                    Container = rldClone
                };

                var component1 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepLabelParameter,
                    ShortName = "name",
                    //Container = step3DGeometryParameter
                    Container = rdl
                };

                var component2 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepIdParameter,
                    ShortName = "id",
                    //Container = step3DGeometryParameter
                    Container = rdl
                };

                var component3 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepLabelParameter,
                    ShortName = "rep_type",
                    //Container = step3DGeometryParameter
                    Container = rdl
                };

                var component4 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepLabelParameter,
                    ShortName = "assembly_label",
                    //Container = step3DGeometryParameter
                    Container = rdl
                };

                var component5 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepIdParameter,
                    ShortName = "assembly_id",
                    //Container = step3DGeometryParameter
                    Container = rdl
                };

                var component6 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepFileRefParameter,
                    ShortName = "source",
                    //Container = step3DGeometryParameter
                    Container = rdl
                };

                // NOTE: Add components here to avoid exception:
                // The CDP4 Services replied with code InternalServerError: Internal Server Error: "exception:
                // The "Component" property of a "CompoundParameterType" is mandatory and must have at least one entry."
                // Exception thrown: 'System.InvalidOperationException' in DEHPSTEPAP242.exe
                // Exception thrown: 'System.InvalidOperationException' in mscorlib.dll
                
                step3DGeometryParameter.Component.Add(component1);
                step3DGeometryParameter.Component.Add(component2);
                step3DGeometryParameter.Component.Add(component3);
                step3DGeometryParameter.Component.Add(component4);
                step3DGeometryParameter.Component.Add(component5);
                step3DGeometryParameter.Component.Add(component6);

                logger.Info($"Adding CompoundParameter: {step3DGeometryParameter.Name} [{step3DGeometryParameter.ShortName}]");

                try
                {
                    // FROM HubController
                    // ==================
                    // public Task CreateOrUpdate<TContainer, TThing>(thing, actionOnClone, deep)
                    // {
                    //   await this.Write(thing, actionOnClone, (transaction, t) => transaction.CreateOrUpdate(t), deep);
                    // }
                    //
                    // private Task Write<TContainer, TThing>(thing, actionOnClone, actionOnTransaction, deep)
                    // {
                    //   var clone = thing.Clone(deep);
                    //   var container = thing.Container.Clone(deep) as TContainer;
                    //   actionOnClone(container, thing);
                    //   
                    //   var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(container), container);
                    //   actionOnTransaction(transaction, clone);
                    //   
                    //   await this.Write(transaction);
                    // }

                    // FROM N.S. --> using internal code
                    // ==================================
                    //
                    // container.ParameterType.Add(step3DGeometryParameter);   // --> looks like ACTIONONCLONE()
                    // 
                    // var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(container), container);
                    // 
                    // 
                    // transaction.CreateOrUpdate(container);                   //
                    // transaction.CreateOrUpdate(step3DGeometryParameter);     //
                    // transaction.CreateOrUpdate(item1);                       //
                    // transaction.CreateOrUpdate(item2);                       //
                    // transaction.CreateOrUpdate(item3);                       // --> looks like ACTIONONTRANSACTION()
                    // transaction.CreateOrUpdate(item4);                       //
                    // transaction.CreateOrUpdate(item5);                       //
                    // transaction.CreateOrUpdate(item6);                       //
                    // 
                    // await this.Write(transaction);


                    await hubController.CreateOrUpdate<ReferenceDataLibrary, CompoundParameterType>(
                        step3DGeometryParameter,
                        (r, p) =>
                        {
                            r.ParameterType.Add(p);

                            // NOTE: These cannot be added because they already added
                            // An exception of type 'System.InvalidOperationException' occurred 
                            // in CDP4Common.dll but was not handled in user code
                            // OrderedItemList already contains the item: 5fd0f44f-fd3f-4e01-a6fd-f7a72b16fa0e
                            //
                            //p.Component.Add(component1);
                            //p.Component.Add(component2);
                            //p.Component.Add(component3);
                            //p.Component.Add(component4);
                            //p.Component.Add(component5);
                            //p.Component.Add(component6);
                        }, true);


#if NOT_WORKING
                    CompoundParameterType geom = step3DGeometryParameter.Clone(false);

                    var thingsToAdd = new List<ParameterTypeComponent>()
                    {
                        component1,
                        component2,
                        component3,
                        component4,
                        component5,
                        component6
                    };

                    await hubController.CreateOrUpdate<ReferenceDataLibrary, ParameterTypeComponent>(
                        thingsToAdd, (c, p) => { Debug.WriteLine($"actionClone for ParameterTypeComponent: {p.ShortName}, Container: {c.Name} "); /* something here? */ }, true);

                    //await hubController.CreateOrUpdate<CompoundParameterType, ParameterTypeComponent>(
                    //    thingsToAdd, (c, p) => { c.Component.Add(p); }, true);
#endif
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    throw e;
                }

                /*
                var container = rdl.Clone(false);
                this.hubController.co


                container.ParameterType.Add(step3DGeometryParameter);

                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(container), container);

                transaction.CreateOrUpdate(container);
                transaction.CreateOrUpdate(step3DGeometryParameter);
                transaction.CreateOrUpdate(item1);
                transaction.CreateOrUpdate(item2);
                transaction.CreateOrUpdate(item3);
                transaction.CreateOrUpdate(item4);
                transaction.CreateOrUpdate(item5);
                transaction.CreateOrUpdate(item6);
                */
            }
        }
    }
}