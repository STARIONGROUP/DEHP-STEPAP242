// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDstLoginViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.ViewModel.Dialogs.Interfaces
{
    using System.Reactive;

    using DEHPCommon.UserInterfaces.Behaviors;

    using ReactiveUI;

    /// <summary>
    /// Interface definiton for <see cref="DstLoginViewModel"/>
    /// </summary>
    public interface IDstLoginViewModel
    {
        /// <summary>
        /// Gets or sets server username value
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Gets or sets server password value
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Gets or sets server uri
        /// </summary>
        string Uri { get; set; }

        /// <summary>
        /// Gets or sets login succesfully flag
        /// </summary>
        bool LoginSuccessfull { get; }

        /// <summary>
        /// Gets or sets an assert whether the specified <see cref="Uri"/> endpoint requires authentication
        /// </summary>
        bool RequiresAuthentication { get; set; }

        /// <summary>
        /// Gets the server login command
        /// </summary>
        ReactiveCommand<Unit> LoginCommand { get; }

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        ICloseWindowBehavior CloseWindowBehavior { get; set; }
    }
}
