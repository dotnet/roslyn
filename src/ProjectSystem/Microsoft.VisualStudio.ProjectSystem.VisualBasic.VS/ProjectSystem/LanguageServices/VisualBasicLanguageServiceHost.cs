// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Integrates the Visual Basic language service with the Visual Basic project system.
    /// </summary>
    [Export(typeof(ICodeModelProvider))]
    [AppliesTo(ProjectCapability.VisualBasicLanguageService)]
    internal class VisualBasicLanguageServiceHost : AbstractLanguageServiceHost
    {
        private static readonly Guid VisualBasicIntelliSenseProvider = new Guid(0xA1B799FA, 0xB147, 0x4999, 0xA8, 0x6E, 0x1F, 0x37, 0x76, 0x5E, 0x6F, 0xB5);

        [ImportingConstructor]
        public VisualBasicLanguageServiceHost(IUnconfiguredProjectVsServices projectVsServices)
            : base(projectVsServices)
        {
        }

        protected override Guid IntelliSenseProviderGuid
        {
            get { return VisualBasicIntelliSenseProvider; }
        }

        /// <summary>
        /// Invoked when the UnconfiguredProject is first loaded to initialize language services.
        /// </summary>
        [UnconfiguredProjectAutoLoad(afterInitialActiveConfigurationKnown: true)]
        [AppliesTo(ProjectCapability.VisualBasicLanguageService)]
        private void Initialize()
        {
            var nowait = this.InitializeAsync();
        }
    }
}
