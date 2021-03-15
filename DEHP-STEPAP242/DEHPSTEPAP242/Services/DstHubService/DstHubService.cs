
namespace DEHPSTEPAP242.Services.DstHubService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Reactive.Linq;
    using System.Linq;
    using System.IO;

    using NLog;

    using CDP4Dal.Operations;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    
    using DEHPCommon.HubController.Interfaces;

    using File = CDP4Common.EngineeringModelData.File;

    /// <summary>
    /// Helper service supporting the work performed by the <see cref="DstController"/> and
    /// also required by different components.
    /// </summary>
    public class DstHubService : IDstHubService
    {
        // File Constants
        private static readonly string APPLICATION_STEP_NAME = "application/step";
        private static readonly string[] APPLICATION_STEP_EXTENSIONS = { "step", "stp" };

        // Parameter Constants
        private static readonly string STEP_ID_UNIT_NAME = "step id";
        private static readonly string STEP_ID_NAME = "step id";
        private static readonly string STEP_LABEL_NAME = "step label";
        private static readonly string STEP_FILE_REF_NAME = "step file reference";
        private static readonly string STEP_GEOMETRY_NAME = "step geometry";
        private static readonly string STEP_GEOMETRY_SHORTNAME = "step_geo";

        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="DEHPCommon.IHubController"/> instance
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// Constructor
        /// </summary>
        public DstHubService(IHubController hubController)
        {
            this.hubController = hubController;
        }

        /// <summary>
        /// Checks/creates all the DST required data is in the Hub.
        /// 
        /// Creates any missing data:
        /// - FileTypes
        /// - ParameterTypes
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task CheckHubDependencies()
        {
            if (this.hubController.OpenIteration is null) return;

            await this.CheckFileTypes();
            await this.CheckParameterTypes();
        }

        /// <summary>
        /// Finds the DST <see cref="CDP4Common.EngineeringModelData.File"/> in the Hub
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.File"/> or null if does not exist</returns>
        public File FindFile(string filePath)
        {
            if (filePath is null || this.hubController.OpenIteration is null)
            {
                return null;
            }

            var name = Path.GetFileNameWithoutExtension(filePath);

            var currentDomainOfExpertise = hubController.CurrentDomainOfExpertise;
            var dfStore = hubController.OpenIteration.DomainFileStore.FirstOrDefault(d => d.Owner == currentDomainOfExpertise);

            var file = dfStore?.File.FirstOrDefault(x => this.IsSTEPFileType(x.CurrentFileRevision) && x.CurrentFileRevision.Name == name);

            return file;
        }

        /// <summary>
        /// Finds the <see cref="CDP4Common.EngineeringModelData.FileRevision"/> from string <see cref="System.Guid"/>
        /// </summary>
        /// <param name="guid">The string value of an <see cref="System.Guid"/></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.FileRevision"/> or null if does not exist</returns>
        public FileRevision FindFileRevision(string guid)
        {
            // NOTE: HubController.GetThingById() does not contemplates file revisions, only FileType.
            //       Local inspection at each File entry is required.

            var currentDomainOfExpertise = this.hubController.CurrentDomainOfExpertise;
            var dfStore = this.hubController.OpenIteration.DomainFileStore.FirstOrDefault(d => d.Owner == currentDomainOfExpertise);

            if (dfStore is null)
            {
                return null;
            }

            var targetIid = new System.Guid(guid);

            foreach (var file in dfStore.File)
            {
                var fileRevision = file.FileRevision.FirstOrDefault(x => x.Iid == targetIid);
                if (fileRevision is { })
                {
                    return fileRevision;
                }
            }

            return null;
        }

        /// <summary>
        /// First compatible STEP <see cref="FileType"/> of a <see cref="FileRevision"/>
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/></param>
        /// <returns>First compatible FileType or null if not</returns>
        public FileType FirstSTEPFileType(FileRevision fileRevision)
        {
            var fileType = fileRevision.FileType.FirstOrDefault(t => (t.Name == APPLICATION_STEP_NAME));
            return fileType;
        }

        /// <summary>
        /// Finds all the revisions for DST files
        /// </summary>
        /// <returns>The <see cref="List{FileRevision}"/> for only current file revision</returns>
        public List<FileRevision> GetFileRevisions()
        {
            if (this.hubController.OpenIteration is null)
            {
                return new List<FileRevision>();
            }

            var currentDomainOfExpertise = hubController.CurrentDomainOfExpertise;
            this.logger.Debug($"Domain of Expertise: { currentDomainOfExpertise.Name }");

            var dfStore = hubController.OpenIteration.DomainFileStore.FirstOrDefault(d => d.Owner == currentDomainOfExpertise);
            this.logger.Debug($"Domain File Store: {dfStore.Name} (Rev: {dfStore.RevisionNumber})");

            var revisions = new List<FileRevision>();

            this.logger.Debug($"Files Count: {dfStore.File.Count}");
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

            return !(fileType is null);
        }

        /// <summary>
        /// Checks if a parameter is compatible with STEP 3D mapping
        /// </summary>
        /// <param name="param">The <see cref="ParameterType"/> to check</param>
        /// <returns>True if it is a candidate for the mapping</returns>
        public bool IsSTEPParameterType(ParameterType param)
        {
            if (param is CompoundParameterType &&
                param.ShortName.Equals("step_geo", StringComparison.CurrentCultureIgnoreCase)
                )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the step geometric parameter where to store a STEP-AP242 part information
        /// </summary>
        /// <returns>A <see cref="ParameterType"/></returns>
        public ParameterType FindSTEPParameterType()
        {
            var rdl = this.GetReferenceDataLibrary();
            var parameters = rdl.ParameterType;

            return parameters.OfType<CompoundParameterType>().FirstOrDefault(x => x.ShortName == STEP_GEOMETRY_SHORTNAME && !x.IsDeprecated);
        }

        /// <summary>
        /// Gets the <see cref="ParameterTypeComponent"/> corresponding to the source file reference
        /// </summary>
        /// <param name="param">The <see cref="ParameterType"/> to check</param>
        /// <returns>A <see cref="ParameterTypeComponent"/> or null if does not contain the component</returns>
        public ParameterTypeComponent FindSourceParameterType(ParameterType param)
        {
            if (param is CompoundParameterType compountParameter)
            {
                return compountParameter.Component.FirstOrDefault(x => x.ShortName == "source");
            }

            return null;
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

            return rdls.First();
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
            var rdl = this.GetReferenceDataLibrary();

            var missingExtensions = new List<string>();

            // Verify that any known STEP extension is checked
            foreach (var ext in APPLICATION_STEP_EXTENSIONS)
            {
                if (!rdl.FileType.Any(t => t.Extension == ext))
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

                    this.logger.Info($"Adding missing STEP FileType {APPLICATION_STEP_NAME} for .{extension}");

                    thingsToWrite.Add(fileType);
                }

                await this.hubController.CreateOrUpdate<ReferenceDataLibrary, FileType>(thingsToWrite, (r, t) => r.FileType.Add(t));
            }
            else
            {
                this.logger.Info($"All STEP FileType already available");
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

            MeasurementUnit oneUnit = units.OfType<SimpleUnit>().FirstOrDefault(u => u.ShortName == "1");
            MeasurementScale stepIdScale = scales.OfType<OrdinalScale>().FirstOrDefault(x => x.Name == STEP_ID_NAME && !x.IsDeprecated);
            ParameterType stepIdParameter = parameters.OfType<SimpleQuantityKind>().FirstOrDefault(x => x.Name == STEP_ID_NAME && !x.IsDeprecated);
            ParameterType stepLabelParameter = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == STEP_LABEL_NAME && !x.IsDeprecated);
            ParameterType stepFileRefParameter = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == STEP_FILE_REF_NAME && !x.IsDeprecated);
            CompoundParameterType step3DGeometryParameter = parameters.OfType<CompoundParameterType>().FirstOrDefault(x => x.Name == STEP_GEOMETRY_NAME && !x.IsDeprecated);

            var rdlClone = rdl.Clone(false);
            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(rdlClone), rdlClone);

            if (oneUnit is null || !(oneUnit is SimpleUnit))
            {
                oneUnit = this.CreateUnit(transaction, rdlClone, STEP_ID_UNIT_NAME, "1");
            }

            if (stepIdScale is null)
            {
                stepIdScale = this.CreateNaturalScale(transaction, rdlClone, STEP_ID_NAME, "-", oneUnit);
            }

            if (stepIdParameter is null)
            {
                stepIdParameter = this.CreateSimpleQuantityParameterType(transaction, rdlClone, STEP_ID_NAME, "step_id", stepIdScale);
            }

            if (stepLabelParameter is null)
            {
                stepLabelParameter = this.CreateTextParameterType(transaction, rdlClone, STEP_LABEL_NAME, "step_label");
            }

            if (stepFileRefParameter is null)
            {
                stepFileRefParameter = this.CreateTextParameterType(transaction, rdlClone, STEP_FILE_REF_NAME, "step_file_reference");
            }

            // Once all sub-parameters exist, compound parameter can be created
            if (step3DGeometryParameter is null)
            {
                var entries = new List<KeyValuePair<string, ParameterType>>()
                {
                    new KeyValuePair<string, ParameterType>("name", stepLabelParameter),
                    new KeyValuePair<string, ParameterType>("id", stepIdParameter),
                    new KeyValuePair<string, ParameterType>("rep_type", stepLabelParameter),
                    new KeyValuePair<string, ParameterType>("assembly_label", stepLabelParameter),
                    new KeyValuePair<string, ParameterType>("assembly_id", stepIdParameter),
                    new KeyValuePair<string, ParameterType>("source", stepFileRefParameter)
                };

                this.CreateCompoundParameter(transaction, rdlClone, STEP_GEOMETRY_NAME, "step_geo", entries);
            }

            transaction.CreateOrUpdate(rdlClone);

            try
            {
                await this.hubController.Write(transaction);
            }
            catch (Exception e)
            {
                this.logger.Error(e);
                this.logger.Error($"Parameter(s) creation failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a <see cref="MeasurementUnit"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <returns><see cref="MeasurementUnit"/></returns>
        private SimpleUnit CreateUnit(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName)
        {
            var newUnit = new SimpleUnit(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
            };

            this.logger.Info($"Adding Unit: {newUnit.Name} [{newUnit.ShortName}]");
            rdlClone.Unit.Add(newUnit);
            transaction.CreateOrUpdate(newUnit);

            return newUnit;
        }

        /// <summary>
        /// Creates a <see cref="MeasurementScale"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="unit">The <see cref="MeasurementUnit"/></param>
        /// <returns><see cref="MeasurementScale"/></returns>
        private OrdinalScale CreateNaturalScale(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName, MeasurementUnit unit)
        {
            var theScale = new OrdinalScale(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
                Unit = unit,
                NumberSet = NumberSetKind.NATURAL_NUMBER_SET,
                MinimumPermissibleValue = "0",
                IsMinimumInclusive = true, // 0 indicates not known value
            };

            this.logger.Info($"Adding Scale: {theScale.Name} [{theScale.ShortName}] Unit={theScale.Unit.Name}");

            rdlClone.Scale.Add(theScale);
            transaction.CreateOrUpdate(theScale);

            return theScale;
        }

        /// <summary>
        /// Creates a <see cref="SimpleQuantityKind"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="scale"><see cref="MeasurementScale"/> set as <see cref="QuantityKind.DefaultScale"/> and <see cref="QuantityKind.PossibleScale"/></param>
        /// <returns><see cref="SimpleQuantityKind"/></returns>
        private SimpleQuantityKind CreateSimpleQuantityParameterType(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName, MeasurementScale scale)
        {
            var theParameter = new SimpleQuantityKind(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
                Symbol = "#",
                DefaultScale = scale,
                PossibleScale = new List<MeasurementScale> { scale },
            };

            this.logger.Info($"Adding Parameter: {theParameter.Name} [{theParameter.ShortName}]");

            rdlClone.ParameterType.Add(theParameter);
            transaction.CreateOrUpdate(theParameter);

            return theParameter;
        }

        /// <summary>
        /// Creates a <see cref="TextParameterType"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <returns><see cref="TextParameterType"/></returns>
        private TextParameterType CreateTextParameterType(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName)
        {
            var theParameter = new TextParameterType(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
                Symbol = "-",
            };

            this.logger.Info($"Adding Parameter: {theParameter.Name} [{theParameter.ShortName}]");

            rdlClone.ParameterType.Add(theParameter);
            transaction.CreateOrUpdate(theParameter);

            return theParameter;
        }

        /// <summary>
        /// Creates a <see cref="CompoundParameterType"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="entries">The <see cref="List{T}"/> with the components to be added</param>
        private void CreateCompoundParameter(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName, List<KeyValuePair<string, ParameterType>> entries)
        {
            var theParameter = new CompoundParameterType(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
                Symbol = "-",
            };

            foreach (var item in entries)
            {
                var component = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ShortName = item.Key,
                    ParameterType = item.Value
                };

                theParameter.Component.Add(component);
                transaction.CreateOrUpdate(component);
            }

            this.logger.Info($"Adding CompoundParameter: {theParameter.Name} [{theParameter.ShortName}]");

            rdlClone.ParameterType.Add(theParameter); 
            transaction.CreateOrUpdate(theParameter);
        }
    }
}
