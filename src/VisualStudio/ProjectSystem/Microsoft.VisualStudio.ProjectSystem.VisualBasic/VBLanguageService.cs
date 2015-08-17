// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.VisualBasic
{
    /// <summary>
    /// Provides integration with the VB language service.
    /// </summary>
    [AppliesTo(ProjectCapabilities.VB + " & " + ProjectCapabilities.LanguageService)]
    [Export(typeof(ICodeModelProvider))]
    internal class VBLanguageService : LanguageServiceBase
    {
        /// <summary>
        /// The VB.NET language service provider.
        /// </summary>
        protected static readonly Guid VBIntellisenseProvider = new Guid(0xA1B799FA, 0xB147, 0x4999, 0xA8, 0x6E, 0x1F, 0x37, 0x76, 0x5E, 0x6F, 0xB5);

        /// <summary>
        /// Initializes a new instance of the <see cref="VBLanguageService"/> class.
        /// </summary>
        [ImportingConstructor]
        public VBLanguageService(UnconfiguredProject unconfiguredProject)
            : base(unconfiguredProject)
        {
        }

        /// <summary>
        /// Gets the GUID of the Intellisense provider to create.
        /// </summary>
        protected override Guid ProviderGuid
        {
            get { return VBIntellisenseProvider; }
        }

        /// <summary>
        /// Invoked when the UnconfiguredProject is first loaded to initialize language services.
        /// </summary>
        [UnconfiguredProjectAutoLoad(afterInitialActiveConfigurationKnown: true)]
        [AppliesTo(ProjectCapabilities.VB + " & " + ProjectCapabilities.LanguageService)]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF")]
        private void Initialize()
        {
            var nowait = this.InitializeAsync();
        }
    }
}
