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

namespace DEHPSTEPAP242.ViewModel.Rows
{
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;

    using ReactiveUI;

    /// <summary>
    /// Represents either a <see cref="ParameterOrOverrideBase"/> or <see cref="Step3DRowViewModel"/>
    /// </summary>
    public class MappedThing : ReactiveObject
    {
        /// <summary>
        /// Backing field for <see cref="Name"/>
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Backing field for <see cref="Value"/>
        /// </summary>
        private object value;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public object Value
        {
            get => this.value;
            set => this.RaiseAndSetIfChanged(ref this.value, value);
        }

        /// <summary>
        /// Backing field for <see cref="GridColumnIndex"/>
        /// </summary>
        private int gridColumnIndex;

        /// <summary>
        /// Gets or sets the grid column index
        /// </summary>
        public int GridColumnIndex
        {
            get => this.gridColumnIndex;
            set => this.RaiseAndSetIfChanged(ref this.gridColumnIndex, value);
        }
    }
}
