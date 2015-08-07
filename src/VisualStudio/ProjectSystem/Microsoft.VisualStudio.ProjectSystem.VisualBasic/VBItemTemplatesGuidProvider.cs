//-----------------------------------------------------------------------
// <copyright file="VBItemTemplatesGuidProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.ProjectSystem.VB.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.VisualStudio.ProjectSystem.Utilities;

    /// <summary>
    /// Implementation of the item templates Guid provider for Visual Basic project system.
    /// </summary>
    [Export(typeof(IAddItemTemplatesGuidProvider))]
    [AppliesTo(ProjectCapabilities.VB)]
    internal class VBItemTemplatesGuidProvider : IAddItemTemplatesGuidProvider
    {
        private static readonly Guid VBProjectType = new Guid("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}");

        private VBItemTemplatesGuidProvider()
        { }

        [Import]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF")]
        private UnconfiguredProject UnconfiguredProject { get; set; }

        /// <summary>
        /// Returns the item templates Guid.
        /// </summary>
        public Guid AddItemTemplatesGuid
        {
            get
            {
                return VBProjectType;
            }
        }
    }
}
