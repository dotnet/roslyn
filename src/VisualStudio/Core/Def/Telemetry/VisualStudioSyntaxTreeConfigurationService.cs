// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(ISyntaxTreeConfigurationService)), Shared]
    internal sealed class VisualStudioSyntaxTreeConfigurationService : ISyntaxTreeConfigurationService
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioSyntaxTreeConfigurationService(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public bool DisableRecoverableTrees
            => _globalOptions.GetOption(OptionsMetadata.DisableRecoverableTrees);

        public bool DisableProjectCacheService
            => _globalOptions.GetOption(OptionsMetadata.DisableProjectCacheService);

        internal sealed class OptionsMetadata
        {
            /// <summary>
            /// Disables if the workspace creates recoverable trees when from its <see cref="ISyntaxTreeFactoryService"/>s.
            /// </summary>
            public static readonly Option2<bool> DisableRecoverableTrees = new(
                "WorkspaceConfigurationOptions", "DisableRecoverableTrees", defaultValue: false,
                new FeatureFlagStorageLocation("Roslyn.DisableRecoverableTrees"));

            public static readonly Option2<bool> DisableProjectCacheService = new(
                "WorkspaceConfigurationOptions", "DisableProjectCacheService", defaultValue: false,
                new FeatureFlagStorageLocation("Roslyn.DisableProjectCacheService"));
        }
    }
}
