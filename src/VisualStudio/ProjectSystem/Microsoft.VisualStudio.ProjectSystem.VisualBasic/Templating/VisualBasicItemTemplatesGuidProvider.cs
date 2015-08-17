// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VisualBasic.Templating
{
    /// <summary>
    /// Implementation of the item templates Guid provider for Visual Basic project system.
    /// </summary>
    //[Export(typeof(IAddItemTemplatesGuidProvider))]
    [AppliesTo(ProjectCapabilities.VB)]
    internal class VisualBasicItemTemplatesGuidProvider : IAddItemTemplatesGuidProvider
    {
        private static readonly Guid VBProjectType = new Guid("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}");

        [ImportingConstructor]
        public VisualBasicItemTemplatesGuidProvider()
        {
        }

        [Import]
        private UnconfiguredProject UnconfiguredProject
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the item templates Guid.
        /// </summary>
        public Guid AddItemTemplatesGuid
        {
            get { return VBProjectType; }
        }
    }
}
