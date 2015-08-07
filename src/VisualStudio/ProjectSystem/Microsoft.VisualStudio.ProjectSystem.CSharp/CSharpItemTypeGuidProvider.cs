//-----------------------------------------------------------------------
// <copyright file="CSharpItemTypeGuidProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.VisualStudio.ProjectSystem.Utilities;

    /// <summary>
    /// Implementation of the item type provider for CSharp project system.
    /// </summary>
    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpItemTypeGuidProvider : IItemTypeGuidProvider
    {
        private static readonly Guid CSharpProjectType = new Guid("{FAE04EC0-301F-11d3-BF4B-00C04F79EFBC}");

        private CSharpItemTypeGuidProvider()
        {}

        [Import]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF")]
        private UnconfiguredProject UnconfiguredProject { get; set; }

        /// <summary>
        /// Returns the item type Guid.
        /// </summary>
        public Guid ProjectTypeGuid
        {
            get
            {
                return CSharpProjectType;
            }
        }
    }
}
