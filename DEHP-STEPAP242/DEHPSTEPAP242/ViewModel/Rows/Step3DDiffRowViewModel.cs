// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Step3DDiffRowViewModel.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
//
//    Authors: Juan Pablo Hernandez Vogt, Ivan Fontaine
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
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using ReactiveUI;
    using STEP3DAdapter;
    using System;
    using System.Collections.Generic;
    using static DEHPSTEPAP242.ViewModel.Interfaces.IDstNodeDiffData;

    /// <summary>
    /// The <see cref="Step3DDiffRowViewModel"/> is the node in the Step comparison tree
    ///
    /// <seealso cref="DstObjectBrowserViewModel"/>
    /// <seealso cref="Builds.HighLevelRepresentationBuilder.HighLevelRepresentationBuilder"/>
    /// </summary>
    public class Step3DDiffRowViewModel : ReactiveObject,IDstNodeDiffData
    {
       

        private Step3DRowData stepRowData;

        #region HLR Tree Indexes

        /// <summary>
        /// Auxiliary index for tree control.
        /// </summary>
        /// <remarks>
        /// It is an unique value in the <see cref="Builds.HighLevelRepresentationBuilder.HighLevelRepresentationBuilder"/> context.
        /// </remarks>

        #endregion HLR Tree Indexes

        #region Part Fields

        /// <summary>
        /// Gets the part instance name
        /// </summary>
        /// <remarks>
        /// The instance is the part name and the usage id <see cref="STEP3D_PartRelation.id"/>
        /// representing a unique string for the part.
        /// </remarks>
        public string InstanceName { get => stepRowData.InstanceName; }

        /// <summary>
        /// Get full path of compised part instance names
        /// </summary>
        public string InstancePath { get => stepRowData.InstancePath; }

        /// <summary>
        /// Get Part name.

        string IDstNodeDiffData.Signature { get => stepRowData.GetSignature(); }
        /// </summary>
        string IDstNodeDiffData.Name { get => stepRowData.Name; }

        /// <summary>
        /// Get short entity type.
        /// </summary>
        public string Type { get => stepRowData.Type; }

        /// <summary>
        /// Get STEP entity type.
        /// </summary>
        public string RepresentationType { get => stepRowData.RepresentationType; }

        /// <summary>
        /// Get STEP entity file Id.
        /// </summary>
        public int StepId { get => stepRowData.StepId; }

        int IDstNodeDiffData.ID { get => stepRowData.ID; set => stepRowData.ID = value; }

        public List<Step3DRowData> Children { get => stepRowData.Children; }
        int IDstNodeDiffData.ParentID { get => stepRowData.ParentID; set => stepRowData.ParentID=value; }

        /// <summary>
        /// Compose a reduced description of the <see cref="STEP3D_Part"/>
        /// </summary>
        public string Description
        {
            get => $"{stepRowData.Type}#{stepRowData.StepId} '{stepRowData.Name}'";
        }

        /// <summary>
        /// Gets a label of association
        /// </summary>
        public string RelationLabel { get => stepRowData.RelationLabel; }

        /// <summary>
        /// Gets the Get STEP entity file Id of the relation (NAUO)
        /// </summary>
        public string RelationId { get => $"{stepRowData.StepId}"; }

        #endregion Part Fields

        
               

        public PartOfKind PartOf { get; set; }
      
        #region Constructor

        public Step3DDiffRowViewModel(Step3DRowData rowdata, IDstNodeDiffData.PartOfKind partOf)
        {
            this.stepRowData = rowdata;
            ((IDstNodeDiffData)this).PartOf = partOf;
          
        }

        #endregion Constructor
    }
}