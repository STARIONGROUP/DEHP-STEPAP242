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

namespace DEHPSTEPAP242.ViewModel.Dialogs.Interfaces
{
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.UserInterfaces.Behaviors;

    using DEHPSTEPAP242.ViewModel.Rows;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for <see cref="MappingConfigurationDialogViewModel"/>
    /// </summary>
    public interface IMappingConfigurationDialogViewModel
    {
        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        ICloseWindowBehavior CloseWindowBehavior { get; set; }

        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        bool IsBusy { get; set; }

        /// <summary>
        /// Sets the target <see cref="Step3DRowViewModel"/> to map
        /// </summary>
        /// <param name="part"></param>
        void SetPart(Step3DRowViewModel part);

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="Step3DRowViewModel"/>
        /// </summary>
        Step3DRowViewModel SelectedThing { get; set; }

        /// <summary>
        /// Gets the collection of the available <see cref="Option"/> from the connected Hub Model
        /// </summary>
        ReactiveList<Option> AvailableOptions { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ElementDefinition"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ElementDefinition> AvailableElementDefinitions { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ElementUsage"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ElementUsage> AvailableElementUsages { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ActualFiniteState"/>s depending on the selected <see cref="Parameter"/>
        /// </summary>
        ReactiveList<ActualFiniteState> AvailableActualFiniteStates { get; }

        /// <summary>
        /// Gets the <see cref="ICommand"/> to continue
        /// </summary>
        ReactiveCommand<object> ContinueCommand { get; set; }
    }
}
