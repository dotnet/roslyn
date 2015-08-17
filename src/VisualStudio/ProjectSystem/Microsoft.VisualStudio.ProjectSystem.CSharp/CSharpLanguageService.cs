// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.VisualStudio.ProjectSystem.Utilities;
    using Microsoft.VisualStudio.ProjectSystem.VS;

    /// <summary>
    /// Provides integration with the C# language service.
    /// </summary>
    [AppliesTo(ProjectCapabilities.CSharp + " & " + ProjectCapabilities.LanguageService)]
    [Export(typeof(ICodeModelProvider))]
    internal class CSharpLanguageService : LanguageServiceBase
    {
        /// <summary>
        /// The C# language service provider.
        /// </summary>
        protected static readonly Guid CSharpIntellisenseProvider = new Guid(0x7D842D0C, 0xFDD6, 0x4e3b, 0x9E, 0x21, 0x0C, 0x26, 0x3F, 0x4B, 0x6E, 0xC2);

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpLanguageService"/> class.
        /// </summary>
        /// <param name="unconfiguredProject">The unconfigured project.</param>
        [ImportingConstructor]
        public CSharpLanguageService(UnconfiguredProject unconfiguredProject)
            : base(unconfiguredProject)
        {
        }

        /// <summary>
        /// Gets the GUID of the Intellisense provider to create.
        /// </summary>
        protected override Guid ProviderGuid
        {
            get { return CSharpIntellisenseProvider; }
        }

        /// <summary>
        /// Invoked when the UnconfiguredProject is first loaded to initialize language services.
        /// </summary>
        [UnconfiguredProjectAutoLoad(afterInitialActiveConfigurationKnown: true)]
        [AppliesTo(ProjectCapabilities.CSharp + " & " + ProjectCapabilities.LanguageService)]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF")]
        private void Initialize()
        {
            var nowait = this.InitializeAsync();
        }
    }
}
