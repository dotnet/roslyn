// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

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

        [ExportSolutionOptionProvider, Shared]
        internal sealed class OptionsMetadata : IOptionProvider
        {
            /// <summary>
            /// Disables if the workspace creates recoverable trees when from its <see cref="ISyntaxTreeFactoryService"/>s.
            /// </summary>
            public static readonly Option<bool> DisableRecoverableTrees = new(
                nameof(WorkspaceConfigurationOptions), nameof(DisableRecoverableTrees), defaultValue: false,
                new FeatureFlagStorageLocation("Roslyn.DisableRecoverableTrees"));

            public static readonly Option<bool> DisableProjectCacheService = new(
                nameof(WorkspaceConfigurationOptions), nameof(DisableProjectCacheService), defaultValue: false,
                new FeatureFlagStorageLocation("Roslyn.DisableProjectCacheService"));

            ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
                DisableRecoverableTrees,
                DisableProjectCacheService);

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public OptionsMetadata()
            {
            }
        }
    }
}
