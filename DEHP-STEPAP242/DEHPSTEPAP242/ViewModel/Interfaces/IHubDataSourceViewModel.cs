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

namespace DEHPSTEPAP242.ViewModel.Interfaces
{
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.PublicationBrowser;
    using ReactiveUI;
    using System.Reactive;

    /// <summary>
    /// Definition of methods and properties of <see cref="HubDataSourceViewModel"/>
    /// </summary>
    public interface IHubDataSourceViewModel
    {
        /// <summary>
        /// The <see cref="IHubBrowserHeaderViewModel"/>
        /// </summary>
        IHubBrowserHeaderViewModel HubBrowserHeader { get; }

        /// <summary>
        /// The <see cref="IObjectBrowserViewModel"/>
        /// </summary>
        IHubObjectBrowserViewModel ObjectBrowser { get; }

        /// <summary>
        /// The <see cref="IPublicationBrowserViewModel"/>
        /// </summary>
        IPublicationBrowserViewModel PublicationBrowser { get; set; }

        /// <summary>
        /// The <see cref="IHubFileStoreBrowserViewModel"/>
        /// </summary>
        IHubFileStoreBrowserViewModel HubFileStoreBrowser { get; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        string ConnectButtonText { get; set; }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for connecting to a data source
        /// </summary>
        ReactiveCommand<object> ConnectCommand { get; set; }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> to refresh the data source
        /// </summary>
        ReactiveCommand<Unit> RefreshCommand { get; set; }
    }
}
