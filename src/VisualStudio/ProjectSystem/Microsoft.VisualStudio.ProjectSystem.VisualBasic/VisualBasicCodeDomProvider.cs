// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.CodeDom.Compiler;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VisualBasic
{
    /// <summary>
    /// Provides the VB CodeDomProvider.
    /// </summary>
    internal class VisualBasicCodeDomProvider
    {
        [ImportingConstructor]
        public VisualBasicCodeDomProvider()
        {
        }

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
