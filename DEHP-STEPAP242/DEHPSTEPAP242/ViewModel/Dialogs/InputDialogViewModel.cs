// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InputDialogViewModel.cs" company="Open Engineering S.A.">
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

namespace DEHPSTEPAP242.ViewModel.Dialogs
{
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;

    /// <summary>
    /// The view-model for the Login that allows users to open a STEP-AP242 file.
    /// </summary>
    class InputDialogViewModel : IInputDialogViewModel
    {
        /// <summary>
        /// Gets the title of the window
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the label for the information asked to the user
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Gets or sets the answer value
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets the message for a null value
        /// </summary>
        public string NullText { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title">The title of the window</param>
        /// <param name="label">The label of the requested information</param>
        /// <param name="text">The initial value of the requested information</param>
        /// <param name="nulltext">The message in case <paramref name="text"/> is empty</param>
        public InputDialogViewModel(string title, string label, string text, string nulltext = null)
        {
            this.Title = string.IsNullOrEmpty(title) ? "Information requested" : title;
            this.Label = string.IsNullOrEmpty(label) ? "Your value:" : label;
            this.Text = text;
            this.NullText = nulltext;
        }
    }
}
