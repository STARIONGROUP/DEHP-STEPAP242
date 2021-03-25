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
    using System.Collections.Generic;

    using DEHPSTEPAP242.ViewModel.Rows;

    ///	<summary>
    /// Interface definition for <see cref="DstObjectBrowserViewModel"/> is the
    /// High Level Representation (aka HLR) of a STEP-AP242 file.
    /// </summary>
    public interface IDstObjectBrowserViewModel
    {
        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        bool IsBusy { get; set; }

        /// <summary>
        /// Self-referential data source content.
        /// 
        /// Using the following service columns:
        /// - Key Field --> Step3DPartTreeNode.ID
        /// - Parent Field --> Step3DPartTreeNode.ParentID
        /// </summary>
        List<Step3DRowViewModel> Step3DHLR { get; }

        /// <summary>
        /// Create the HLR tree from the Parts/Relations.
        /// </summary>
        /// <param name="parts">List of geometric parts</param>
        /// <param name="relations">List of part relations defining instances in the tree composition</param>
        void UpdateHLR();
    }
}
