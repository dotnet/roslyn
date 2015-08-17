// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.Implementation
{
    using System.CodeDom.Compiler;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.VisualStudio.ProjectSystem.Utilities;

    /// <summary>
    /// Provides the CSharp CodeDomProvider.
    /// </summary>
    internal class CSharpCodeDomProvider
    {
        /// <summary>
        /// Gets the unconfigured project.
        /// </summary>
        /// <value>
        /// The unconfigured project.
        /// </value>
        [Import]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF.")]
        protected UnconfiguredProject UnconfiguredProject { get; private set; }

        /// <summary>
        /// Gets the CodeDomProvider.
        /// </summary>
        /// <value>
        /// The CodeDomProvider.
        /// </value>
        [ExportVsProfferedProjectService(typeof(CodeDomProvider))]
        [AppliesTo(ProjectCapabilities.CSharp)]
        private CodeDomProvider CodeDomProviderService
        {
            get { return CodeDomProvider.CreateProvider("CSharp"); }
        }
    }
}
