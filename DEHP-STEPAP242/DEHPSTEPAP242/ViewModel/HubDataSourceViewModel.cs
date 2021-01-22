// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubDataSourceViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
// 
//    This file is part of DEHPSTEPAP242
// 
//    The DEHPSTEPAP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHPSTEPAP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPSTEPAP242.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Threading.Tasks;

    using CDP4Common.SiteDirectoryData;
    using CDP4Common.CommonData;
    using CDP4Common.Types;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPSTEPAP242.ViewModel.Interfaces;

    using ReactiveUI;
    using System.Collections.Generic;

    /// <summary>
    /// View model that represents a data source panel which holds a tree like browser, a informational header and
    /// some control regarding the connection to the data source
    /// </summary>
    public sealed class HubDataSourceViewModel : DataSourceViewModel, IHubDataSourceViewModel
    {
        private const string RDL_NAME = "Generic ECSS-E-TM-10-25 Reference Data Library";
        private const string APPLICATION_TYPE_NAME = "application/step";

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IObjectBrowserTreeSelectorService"/>
        /// </summary>
        private readonly IObjectBrowserTreeSelectorService treeSelectorService;

        /// <summary>
        /// The <see cref="IObjectBrowserViewModel"/>
        /// </summary>
        public IObjectBrowserViewModel ObjectBrowser { get; set; }

        /// <summary>
        /// The <see cref="IHubBrowserHeaderViewModel"/>
        /// </summary>
        public IHubBrowserHeaderViewModel HubBrowserHeader { get; set; }

        /// <summary>
        /// The <see cref="IHubFileStoreBrowserViewModel"/>
        /// </summary>
        public IHubFileStoreBrowserViewModel HubFileStoreBrowser { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="HubDataSourceViewModel"/>
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="objectBrowser">The <see cref="IObjectBrowserViewModel"/></param>
        /// <param name="treeSelectorService">The <see cref="IObjectBrowserTreeSelectorService"/></param>
        /// <param name="hubBrowserHeader">The <see cref="IHubBrowserHeaderViewModel"/></param>
        public HubDataSourceViewModel(INavigationService navigationService, IHubController hubController, IObjectBrowserViewModel objectBrowser,
            IObjectBrowserTreeSelectorService treeSelectorService, IHubBrowserHeaderViewModel hubBrowserHeader, IHubFileStoreBrowserViewModel hubFileBrowser) : base(navigationService)
        {
            this.hubController = hubController;
            this.treeSelectorService = treeSelectorService;
            this.ObjectBrowser = objectBrowser;
            this.HubBrowserHeader = hubBrowserHeader;
            this.HubFileStoreBrowser = hubFileBrowser;
            
            InitializeCommands();
        }

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            this.ConnectCommand = ReactiveCommand.Create();
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());
        }

        /// <summary>
        /// The connect text for the connect button
        /// </summary>
        private const string ConnectText = "Connect";

        /// <summary>
        /// The disconnect text for the connect button
        /// </summary>
        private const string DisconnectText = "Disconnect";

        /// <summary>
        /// Backing field for <see cref="ConnectButtonText"/>
        /// </summary>
        private string connectButtonText = ConnectText;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string ConnectButtonText
        {
            get => this.connectButtonText;
            set => this.RaiseAndSetIfChanged(ref this.connectButtonText, value);
        }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for connecting to a data source
        /// </summary>
        public ReactiveCommand<object> ConnectCommand { get; set; }

        /// <summary>
        /// Executes the <see cref="HubDataSourceViewModel.ConnectCommand"/>
        /// </summary>
        void ConnectCommandExecute()
        {
            if (this.hubController.IsSessionOpen)
            {
                this.hubController.Close();
            }
            else
            {
                this.NavigationService.ShowDialog<Login>();
            }

            this.UpdateConnectButtonText(this.hubController.IsSessionOpen);

            if (hubController.IsSessionOpen)
            {
                CheckHubDependencies();
            }
        }

        /// <summary>
        /// Updates the <see cref="ConnectButtonText"/>
        /// </summary>
        /// <param name="isSessionOpen">Assert whether the the button text should be <see cref="ConnectText"/> or <see cref="DisconnectText"/></param>
        void UpdateConnectButtonText(bool isSessionOpen)
        {
            this.ConnectButtonText = isSessionOpen ? DisconnectText : ConnectText;
        }

        /// <summary>
        /// Loads a STEP file stored in the server.
        /// </summary>
        protected override void LoadFileCommandExecute()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks that objects 
        /// </summary>
        public void CheckHubDependencies()
        {
            if (hubController.OpenIteration is null) return;

            CheckFileTypes();
            CheckParameterTypes();
        }

        /// <summary>
        /// Check that STEP <see cref="FileType"/> exists in the Generic RDL
        /// 
        /// Two <see cref="FileType"/> are used:
        /// - application/step for .step extension
        /// - application/step for .stp extension
        /// </summary>
        public async void CheckFileTypes()
        {
            // Workflow:
            // 1. Get current file types   | Where? iteration, if many exists?
            // 2. Add if does not exist    |
            //
            // Current example server contains:
            // RDL: RDL specific to CDF_generic_template --> contains 0 FileTypes
            // RDL: Generic ECSS-E-TM-10-25 Reference Data Library --> contains 28 FileTypes

            var iteration = hubController.OpenIteration;

            Debug.WriteLine("=====");
            Debug.WriteLine("iteration.RequiredRdls");
            foreach (var rdl in iteration.RequiredRdls)
            {
                Debug.WriteLine($"RDL: {rdl.Name} --> contains {rdl.FileType.Count} FileTypes");

                //var stepFileTypes = rdl.FileType.Where(t => t.Extension == "step" || t.Extension == "stp");
                //Debug.WriteLine($"RDL FileType: with step types {stepFileTypes.Count()}");
            }

#if CHECK_OTHER_RDLS_FILETYPES
            var domain = hubController.CurrentDomainOfExpertise;
            var site = hubController.GetSiteDirectory();

            Debug.WriteLine("=====");
            Debug.WriteLine("domain.RequiredRdls");
            foreach (var rdl in domain.RequiredRdls)
            {
                Debug.WriteLine($"RDL: {rdl.Name} --> contains {rdl.FileType.Count} FileTypes");

                var stepFileTypes = rdl.FileType.Where(t => t.Extension == "step" || t.Extension == "stp");

                Debug.WriteLine($"RDL FileType: with step types {stepFileTypes.Count()}");

                //foreach (var ft in rdl.FileType)
                //{
                //    
                //
                //    if (ft.Name == APPLICATION_TYPE_NAME)
                //	{
                //        if (ft.Extension == "step") hasFileTypeStep = true;
                //        else if (ft.Extension == "stp") hasFileTypeStp = true;
                //    }
                //}   
            }
#endif

            var RDL = iteration.RequiredRdls.FirstOrDefault(rdl => rdl.Name == RDL_NAME);

            if (RDL is null)
            {
                Debug.WriteLine("ERROR: RDL does not exists");
                return;
            }

            var missingExtensions = new List<string>();

            // Verify that any STEP well known extension is checked
            foreach (var ext in new List<string>() { "step", "stp" })
            {
                if (RDL.FileType.Any(t => t.Extension == ext) == false)
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
                        Name = APPLICATION_TYPE_NAME,
                        ShortName = APPLICATION_TYPE_NAME,
                        Extension = extension,
                        Container = RDL
                    };

                    Debug.WriteLine($"Adding missing STEP FileType {APPLICATION_TYPE_NAME} for .{extension}");

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
        private void CheckParameterTypes()
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

            var site = hubController.GetSiteDirectory();
            
            Debug.WriteLine("=====");
            Debug.WriteLine("site.SiteReferenceDataLibrary");

            var srdl = site.SiteReferenceDataLibrary.FirstOrDefault(r => r.Name == RDL_NAME);

            var units = srdl.Unit;
            var scales = srdl.Scale;
            var parameters = srdl.ParameterType;

            //foreach (var item in scales)
            //{
            //    Debug.WriteLine($"  Scale: {item.Name} [{item.ShortName}] {item.NumberSet}");
            //}
            //
            //foreach (var item in units)
            //{
            //    Debug.WriteLine($"  Uniy: {item.Name} ({item.ShortName})");
            //}

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
                    Container = srdl
                };
            
                Debug.WriteLine($"Adding Scale: {stepIdScale.Name} [{stepIdScale.ShortName}] Unit={stepIdScale.Unit.Name}");
            
                hubController.CreateOrUpdate<SiteReferenceDataLibrary, MeasurementScale>(
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
                    Container = srdl
                };

                Debug.WriteLine($"Adding Parameter: {stepIdParameter.Name} [{stepIdParameter.ShortName}]");

                hubController.CreateOrUpdate<SiteReferenceDataLibrary, ParameterType>(
                    stepIdParameter, (r, p) => r.ParameterType.Add(p));
            }

            if (stepLabelParameter is null)
            {
                stepLabelParameter = new TextParameterType(Guid.NewGuid(), null, null)
                {
                    Name = STEP_LABEL_NAME,
                    ShortName = "step_label",
                    Symbol = "-",
                    Container = srdl
                };

                Debug.WriteLine($"Adding Parameter: {stepLabelParameter.Name} [{stepLabelParameter.ShortName}]");

                hubController.CreateOrUpdate<SiteReferenceDataLibrary, ParameterType>(
                    stepLabelParameter, (r, p) => r.ParameterType.Add(p));
            }

            if (stepFileRefParameter is null)
            {
                stepFileRefParameter = new TextParameterType(Guid.NewGuid(), null, null)
                {
                    Name = STEP_FILE_REF_NAME,
                    ShortName = "step_file_reference",
                    Symbol = "-",
                    Container = srdl
                };

                Debug.WriteLine($"Adding Parameter: {stepFileRefParameter.Name} [{stepFileRefParameter.ShortName}]");

                hubController.CreateOrUpdate<SiteReferenceDataLibrary, ParameterType>(
                    stepFileRefParameter, (r, p) => r.ParameterType.Add(p));
            }

            if (step3DGeometryParameter is null)
            {
                step3DGeometryParameter = new CompoundParameterType(Guid.NewGuid(), null, null)
                {
                    Name = STEP_GEOMETRY_NAME,
                    ShortName = "step_geo",
                    Symbol = "-",
                    Container = srdl
                };

                var item1 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepLabelParameter,
                    ShortName = "name",
                    Container = step3DGeometryParameter
                };

                var item2 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepIdParameter,
                    ShortName = "id",
                    Container = step3DGeometryParameter
                };

                var item3 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepLabelParameter,
                    ShortName = "rep_type",
                    Container = step3DGeometryParameter
                };

                var item4 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepLabelParameter,
                    ShortName = "assembly_label",
                    Container = step3DGeometryParameter
                };

                var item5 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepIdParameter,
                    ShortName = "assembly_id",
                    Container = step3DGeometryParameter
                };

                var item6 = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ParameterType = stepFileRefParameter,
                    ShortName = "source",
                    Container = step3DGeometryParameter
                };

                //step3DGeometryParameter.Component.Add(item1);
                //step3DGeometryParameter.Component.Add(item2);
                //step3DGeometryParameter.Component.Add(item3);
                //step3DGeometryParameter.Component.Add(item4);
                //step3DGeometryParameter.Component.Add(item5);
                //step3DGeometryParameter.Component.Add(item6);

                Debug.WriteLine($"Adding Parameter: {step3DGeometryParameter.Name} [{step3DGeometryParameter.ShortName}]");

                hubController.CreateOrUpdate<SiteReferenceDataLibrary, CompoundParameterType>(
                    step3DGeometryParameter,
                    (r, p) => {
                        r.ParameterType.Add(p);

                        p.Component.Add(item1);
                        p.Component.Add(item2);
                        p.Component.Add(item3);
                        p.Component.Add(item4);
                        p.Component.Add(item5);
                        p.Component.Add(item6);
                    }, true);
            }
        }
    }
}
