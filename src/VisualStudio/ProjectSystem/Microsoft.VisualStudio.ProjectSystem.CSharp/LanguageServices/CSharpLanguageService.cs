// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.LanguageServices
{
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
        private void Initialize()
        {
            var nowait = this.InitializeAsync();
        }
    }
}
