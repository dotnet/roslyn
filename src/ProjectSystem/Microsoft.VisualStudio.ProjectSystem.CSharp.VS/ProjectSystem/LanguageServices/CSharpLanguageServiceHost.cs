// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Integrates the C# language service with the C# project system.
    /// </summary>
    [Export(typeof(ICodeModelProvider))]
    [AppliesTo(ProjectCapability.CSharpLanguageService)]
    internal class CSharpLanguageServiceHost : AbstractLanguageServiceHost
    {
        private static readonly Guid CSharpIntellisenseProvider = new Guid(0x7D842D0C, 0xFDD6, 0x4e3b, 0x9E, 0x21, 0x0C, 0x26, 0x3F, 0x4B, 0x6E, 0xC2);

        [ImportingConstructor]
        public CSharpLanguageServiceHost(IUnconfiguredProjectVsServices projectVsServices)
            : base(projectVsServices)
        {
        }

        protected override Guid IntelliSenseProviderGuid
        {
            get { return CSharpIntellisenseProvider; }
        }

        /// <summary>
        /// Invoked when the UnconfiguredProject is first loaded to initialize language services.
        /// </summary>
        [UnconfiguredProjectAutoLoad(afterInitialActiveConfigurationKnown: true)]
        [AppliesTo(ProjectCapability.CSharpLanguageService)]
        private void Initialize()
        {
            var nowait = InitializeAsync();
        }
    }
}
