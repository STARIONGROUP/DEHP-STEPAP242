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

namespace DEHPSTEPAP242
{
    using Autofac;
    using DEHPCommon;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.Services.FileStoreService;
    using DEHPSTEPAP242.Settings;
    using DEHPSTEPAP242.ViewModel;
    using DEHPSTEPAP242.ViewModel.Dialogs;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.ViewModel.NetChangePreview;
    using DEHPSTEPAP242.Views;
    using DevExpress.Xpf.Core;
    using NLog;
    using System;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Threading;
    using DXSplashScreenViewModel = DevExpress.Mvvm.DXSplashScreenViewModel;
    using SplashScreen = DEHPCommon.UserInterfaces.Views.SplashScreen;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// The <see cref="NLog"/> logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new <see cref="App"/>
        /// </summary>
        /// <param name="containerBuilder">An optional <see cref="Container"/></param>
        public App(ContainerBuilder containerBuilder = null)
        {
            this.LogAppStart();

            this.Exit += this.OnExit;
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomainUnhandledException;

            var splashScreenViewModel = new DXSplashScreenViewModel() { Title = "DEHP STEP-AP242 Adapter", Logo = new Uri("pack://application:,,,/Resources/cdplogo3d_48x48.png") };
            SplashScreenManager.Create(() => new SplashScreen(), splashScreenViewModel).ShowOnStartup();
            containerBuilder ??= new ContainerBuilder();
            RegisterTypes(containerBuilder);
            RegisterViewModels(containerBuilder);
            AppContainer.BuildContainer(containerBuilder);
        }

        /// <summary>
        /// Writes stating log message
        /// </summary>
        private void LogAppStart()
        {
            this.logger.Info("--------------------------------------------------------");
            this.logger.Info($"Starting STEP-AP242 Adapter {Assembly.GetExecutingAssembly().GetName().Version}");
            this.logger.Info("--------------------------------------------------------");
        }

        /// <summary>
        /// Handles dispatcher unhandled exception
        /// </summary>
        /// <param name="sender">The <see cref="object"/> sender</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/></param>
        public void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            this.logger.Error(e.Exception);
            e.Handled = true;
        }

        /// <summary>
        /// Warn when an exception is thrown and log it 
        /// </summary>
        /// <param name="sender">The <see cref="object"/> sender</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/></param>
        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var errorMessage = $"{sender} has thrown {e.ExceptionObject.GetType()} \n\r {(e.ExceptionObject as Exception)?.Message}";
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            this.logger.Error(e.ExceptionObject);
        }

        /// <summary>
        /// Occurs when the app closes, it makes sure any opc connection are properly closed
        /// </summary>
        /// <param name="sender">The <see cref="object"/> sender</param>
        /// <param name="e">The <see cref="ExitEventArgs"/></param>
        private void OnExit(object sender, ExitEventArgs e)
        {
            this.logger.Info("--------------------------------------------------------");
            this.logger.Info("Leaving application");
            this.logger.Info("--------------------------------------------------------");
        }

        /// <summary>
        /// Occurs when <see cref="Application"/> starts, starts a new <see cref="ILifetimeScope"/> and open the <see cref="Application.MainWindow"/>
        /// </summary>
        /// <param name="e">The <see cref="StartupEventArgs"/></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            using (var scope = AppContainer.Container.BeginLifetimeScope())
            {
                scope.Resolve<INavigationService>().Show<MainWindow>();
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// Registers the types that can be resolved by the <see cref="IContainer"/>
        /// </summary>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/></param>
        private static void RegisterTypes(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<DstController.DstController>().As<IDstController>().SingleInstance();
            containerBuilder.RegisterType<FileStoreService>().As<IFileStoreService>().SingleInstance();
            containerBuilder.RegisterType<DstHubService>().As<IDstHubService>().SingleInstance();
            containerBuilder.RegisterType<UserPreferenceService<AppSettings>>().As<IUserPreferenceService<AppSettings>>().SingleInstance();
            containerBuilder.RegisterType<MappingEngine>().As<IMappingEngine>().WithParameter(MappingEngine.ParameterName, Assembly.GetExecutingAssembly());
            containerBuilder.RegisterType<HighLevelRepresentationBuilder>().As<IHighLevelRepresentationBuilder>();
        }

        /// <summary>
        /// Registers all the view model so the depencies can be injected
        /// </summary>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/></param>
        private static void RegisterViewModels(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<MainWindowViewModel>().As<IMainWindowViewModel>().SingleInstance();
            // Hub
            containerBuilder.RegisterType<HubDataSourceViewModel>().As<IHubDataSourceViewModel>().SingleInstance();
            containerBuilder.RegisterType<HubObjectBrowserViewModel>().As<IHubObjectBrowserViewModel>().SingleInstance();
            containerBuilder.RegisterType<HubFileStoreBrowserViewModel>().As<IHubFileStoreBrowserViewModel>().SingleInstance();
            containerBuilder.RegisterType<HubNetChangePreviewViewModel>().As<IHubNetChangePreviewViewModel>().SingleInstance();
            // Dst
            containerBuilder.RegisterType<DstBrowserHeaderViewModel>().As<IDstBrowserHeaderViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstObjectBrowserViewModel>().As<IDstObjectBrowserViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstDataSourceViewModel>().As<IDstDataSourceViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstLoadFileViewModel>().As<IDstLoadFileViewModel>();
            containerBuilder.RegisterType<MappingConfigurationDialogViewModel>().As<IMappingConfigurationDialogViewModel>();
            containerBuilder.RegisterType<MappingConfigurationManagerDialogViewModel>().As<IMappingConfigurationManagerDialogViewModel>();
            containerBuilder.RegisterType<MappingViewModel>().As<IMappingViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstTransferControlViewModel>().As<ITransferControlViewModel>().SingleInstance();
        }
    }
}
