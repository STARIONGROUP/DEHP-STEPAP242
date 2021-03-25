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

namespace DEHPSTEPAP242.MappingRules
{
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using DEHPSTEPAP242.ViewModel.Rows;

    /// <summary>
    /// Helper class which keeps a reference to the <see cref="ValueArray{string}"/> 
    /// that needs to me updated with the new <see cref="FileRevision"/> of the source
    /// STEP 3D file in the Hub.
    /// </summary>
    public class Step3DTargetSourceParameter
    {
        /// <summary>
        /// The <see cref="Step3DRowViewModel"/> originating the change
        /// </summary>
        public readonly Step3DRowViewModel part;

        /// <summary>
        /// The <see cref="ValueArray{string}"/> of the <see cref="IValueSet"/> of interest
        /// </summary>
        private readonly ValueArray<string> values;

        /// <summary>
        /// The index in the <see cref="ValueArray{string}"/> for the <see cref="ParameterTypeComponent"/> corresponding to the "source" field
        /// </summary>
        private readonly int componentIndex;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/></param>
        /// <param name="values">The <see cref="ValueArray{string}"/> of the target <see cref="IValueSet"/></param>
        /// <param name="componentIndex">The index corresponding to the source field</param>
        public Step3DTargetSourceParameter(Step3DRowViewModel part, ValueArray<string> values, int componentIndex)
        {
            this.part = part;
            this.values = values;
            this.componentIndex = componentIndex;
        }

        /// <summary>
        /// Updates the <see cref="ValueArray{string}"/> associated to the source parameter
        /// </summary>
        /// <param name="fileRevision"></param>
        public void UpdateSource(FileRevision fileRevision)
        {
            this.values[componentIndex] = fileRevision.Iid.ToString();
        }
    }
}
