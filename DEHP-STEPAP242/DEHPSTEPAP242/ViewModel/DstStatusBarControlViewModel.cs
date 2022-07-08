// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstStatusBarControlViewModel.cs" company="Open Engineering S.A.">
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
using DEHPCommon.Enumerators;
using DEHPCommon.Services.NavigationService;
using DEHPCommon.UserInterfaces.ViewModels;
using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
using DEHPSTEPAP242.ViewModel.Interfaces;
using DEHPSTEPAP242.Views.Dialogs;
using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DEHPSTEPAP242.ViewModel
{
    public class DstStatusBarControlViewModel:StatusBarControlViewModel
    {

        private readonly IDstUserSettingsViewModel dstUserSettingsViewModel;


      public DstStatusBarControlViewModel(INavigationService inav,IDstUserSettingsViewModel usvm):base(inav)
        {
            this.dstUserSettingsViewModel = usvm;

            this.UserSettingCommand = ReactiveCommand.Create();
            this.UserSettingCommand.Subscribe(_ => this.ExecuteUserSettingCommand());

        }

        /// <summary>
        /// Executes the <see cref="UserSettingCommand"/>
        /// </summary>
        protected override void ExecuteUserSettingCommand()
        {
            Append("Opening the user settings dialog box");
            this.dstUserSettingsViewModel.HandleUserSettings();
        }
    }
    
}
