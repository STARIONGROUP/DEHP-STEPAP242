// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DateSourceViewModel.cs" company="Open Engineering S.A.">
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

namespace DEHPSTEPAP242.ViewModel
{
    using DEHPCommon.Services.NavigationService;
    using ReactiveUI;
    using System;

    /// <summary>
    /// The <see cref="DataSourceViewModel"/> is the base view model for view model that represents a data source like <see cref="DstDataSourceViewModel"/>
    /// </summary>
    public abstract class DataSourceViewModel : ReactiveObject
    {
        /// <summary>
        /// The Load text for the load button
        /// </summary>
        private const string LoadText = "Load STEP-AP242 file";

        private string loadButtonText = LoadText;

        /// <summary>
        /// Gets the <see cref="INavigationService"/>
        /// </summary>
        protected readonly INavigationService NavigationService;

        /// <summary>
        /// Initializes a new <see cref="DataSourceViewModel"/>
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        protected DataSourceViewModel(INavigationService navigationService)
        {
            this.NavigationService = navigationService;
        }

        /// <summary>
        /// Initializes the <see cref="ReactiveCommand{T}"/>
        /// </summary>
        protected virtual void InitializeCommands()
        {
            this.LoadFileCommand = ReactiveCommand.Create();
            this.LoadFileCommand.Subscribe(_ => this.LoadFileCommandExecute());
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string LoadButtonText
        {
            get => this.loadButtonText;
            set => this.RaiseAndSetIfChanged(ref this.loadButtonText, value);
        }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for loadind a STEP file source
        /// </summary>
        public ReactiveCommand<object> LoadFileCommand { get; set; }

        /// <summary>
        /// Executes the <see cref="LoadFileCommand"/>
        /// </summary>
        protected abstract void LoadFileCommandExecute();
    }
}
