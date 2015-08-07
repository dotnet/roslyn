//-----------------------------------------------------------------------
// <copyright file="VBCodeDomProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.ProjectSystem.VB.Implementation
{
    using System.CodeDom.Compiler;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.VisualStudio.ProjectSystem.Utilities;

    /// <summary>
    /// Provides the VB CodeDomProvider.
    /// </summary>
    internal class VBCodeDomProvider
    {
        /// <summary>
        /// Gets the unconfigured project.
        /// </summary>
        [Import]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF.")]
        protected UnconfiguredProject UnconfiguredProject { get; private set; }

        /// <summary>
        /// Gets the CodeDomProvider.
        /// </summary>
        [ExportVsProfferedProjectService(typeof(CodeDomProvider))]
        [AppliesTo(ProjectCapabilities.VB)]
        private CodeDomProvider CodeDomProviderService
        {
            get { return CodeDomProvider.CreateProvider("VB"); }
        }
    }
}
