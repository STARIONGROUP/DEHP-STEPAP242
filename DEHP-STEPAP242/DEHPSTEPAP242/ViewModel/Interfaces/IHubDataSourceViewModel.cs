// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHubDataSourceViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.ViewModel.Interfaces
{
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// Definition of methods and properties of <see cref="HubDataSourceViewModel"/>
    /// </summary>
    public interface IHubDataSourceViewModel
    {
        /// <summary>
        /// The <see cref="IObjectBrowserViewModel"/>
        /// </summary>
        IObjectBrowserViewModel ObjectBrowser { get; }

        /// <summary>
        /// The <see cref="IHubBrowserHeaderViewModel"/>
        /// </summary>
        IHubBrowserHeaderViewModel HubBrowserHeader { get; }

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
    }
}
