// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Step3DViewModel.cs" company="Open Engineering S.A.">
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
using DEHPSTEPAP242.ViewModel.Interfaces;
using STEP3DAdapter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DEHPSTEPAP242.ViewModel.Rows
{
    public class Step3DRowData
    {         
        public STEP3D_Part Part { get; }

        private STEP3D_PartRelation Relation { get; }

        public int ID { get; set; }

        /// <summary>
        /// Auxiliary parent index for tree control.
        /// </summary>
        public int ParentID { get ; set; }

        /// <summary>
        /// Gets the part instance name
        /// </summary>
        /// <remarks>
        /// The instance is the part name and the usage id <see cref="STEP3D_PartRelation.id"/>
        /// representing a unique string for the part.
        /// </remarks>
        /// 

        public Step3DRowData Parent{ get; set; }
        

        /**<summary> Use to store a unique name made by using the name and a numeral suffix in case of several node having the same name
         * </summary>
         */
        public string UniqueName { get; set; }



        public string InstanceName { get; private set; }

        /// <summary>
        /// Get full path of compised part instance names
        /// </summary>
        public string InstancePath { get; private set; }

        /// <summary>
        /// Get Part name.
        /// </summary>
        public string Name { get => Part.name;}

        /// <summary>
        /// Get short entity type.
        /// </summary>
        public string Type { get => Part.type; }

        /// <summary>
        /// Get STEP entity type.
        /// </summary>
        public string RepresentationType { get => Part.representation_type; }

        /// <summary>
        /// Get STEP entity file Id.
        /// </summary>
        public int StepId { get => Part.stepId; }

        /// <summary>
        /// Compose a reduced description of the <see cref="STEP3D_Part"/>
        /// </summary>
        public string Description
        {
            get => $"{Part.type}#{Part.stepId} '{Part.name}'";            
        }

        /// <summary>
        /// Gets a label of association
        /// </summary>
        /// <remarks>
        /// Using as label the <see cref="STEP3D_PartRelation.id"/> instead 
        /// <see cref="STEP3D_PartRelation.name"/> because it was the only unique value 
        /// exported by the different CAD applications tested during developments.
        /// </remarks>
       
        public string RelationLabel
        {
            get => $"{Relation?.id}";
        }
            
            

        /// <summary>
        /// Gets the Get STEP entity file Id of the relation (NAUO)
        /// </summary>
        public string RelationId { get => $"{Relation?.stepId}"; }

    /** <summary>
     * Retrieves the signature of the node. It is basically the full path of the node, made using uniquenames.
     * </summary>
     */
        public string GetSignature()
        {
            string me = this.UniqueName;

            if (this.Parent != null)
            {
                me =  Parent.GetSignature()+"/"+me;
            }

            return me;
        }


        public Step3DRowData(Dictionary<string,int> namedict,STEP3D_Part part, STEP3D_PartRelation relation,string parentPath="")
        {
            this.Part = part;
            this.Relation = relation;
            this.UniqueName = this.Name;
            int namecnt = 1;
            if (namedict != null)
            {
                if (namedict.TryGetValue(this.Name, out namecnt))
                {
                    string suffix = namecnt.ToString();
                    this.UniqueName += suffix;
                    namecnt++;
                }
                namedict[this.Name] = namecnt;
            }

            
            
            this.InstanceName = string.IsNullOrWhiteSpace(this.RelationLabel) ?this.Name : $"{this.Name}({this.RelationLabel})";
            this.InstancePath = string.IsNullOrWhiteSpace(parentPath) ? this.InstanceName : $"{parentPath}.{this.InstanceName}";
            
        }

    }
}
