// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis
{
    [ExportSolutionOptionProvider, Shared]
    internal class WorkspaceConfigurationOptions : IOptionProvider
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

        /// <summary>
        /// This option allows the user to enable this. We are putting this behind a feature flag for now since we could have extensions
        /// surprised by this and we want some time to work through those issues.
        /// </summary>
        internal static readonly Option2<bool?> EnableOpeningSourceGeneratedFilesInWorkspace = new(nameof(WorkspaceConfigurationOptions), nameof(EnableOpeningSourceGeneratedFilesInWorkspace), defaultValue: null,
            new RoamingProfileStorageLocation("TextEditor.Roslyn.Specific.EnableOpeningSourceGeneratedFilesInWorkspaceExperiment"));

        internal static readonly Option2<bool> EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag = new(nameof(WorkspaceConfigurationOptions), nameof(EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.SourceGeneratorsEnableOpeningInWorkspace"));

        internal static bool ShouldConnectSourceGeneratedFilesToWorkspace(OptionSet options)
        {
            return options.GetOption(EnableOpeningSourceGeneratedFilesInWorkspace) ??
                options.GetOption(EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag);
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            DisableRecoverableTrees,
            DisableProjectCacheService,
            EnableOpeningSourceGeneratedFilesInWorkspace,
            EnableOpeningSourceGeneratedFilesInWorkspaceFeatureFlag);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceConfigurationOptions()
        {
        }
    }
}
