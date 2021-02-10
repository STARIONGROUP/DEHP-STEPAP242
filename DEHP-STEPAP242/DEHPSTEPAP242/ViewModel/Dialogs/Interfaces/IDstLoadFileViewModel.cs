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
    using DEHPCommon.UserInterfaces.Behaviors;

    using ReactiveUI;
    using System.Reactive;

    /// <summary>
    /// Interface definiton for <see cref="DstLoadFileViewModel"/>
    /// </summary>
    public interface IDstLoadFileViewModel
    {
        /// <summary>
        /// Current path to a STEP file
        /// </summary>
        string FilePath { get; set; }

        /// <summary>
        /// Load STEP-AP242 file.
        /// 
        /// Uses the <see cref="FilePath"/> value.
        /// </summary>
        ReactiveCommand<Unit> LoadFileCommand { get; }

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        ICloseWindowBehavior CloseWindowBehavior { get; set; }
    }
}
