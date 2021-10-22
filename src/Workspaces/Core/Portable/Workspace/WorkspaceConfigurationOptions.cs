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
        /// Disables holding onto the assembly references for runtime (not user/nuget/etc.) dlls weakly.
        /// </summary>
        public static readonly Option<bool> DisableReferenceManagerWeakRuntimeReferences = new(
            nameof(WorkspaceConfigurationOptions), nameof(DisableReferenceManagerWeakRuntimeReferences), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.DisableReferenceManagerWeakRuntimeReferences"));

        /// <summary>
        /// Disables holding onto the assembly references for runtime (not user/nuget/etc.) dlls weakly.
        /// </summary>
        public static readonly Option<bool> DisableCompilationTrackerWeakCompilationReferences = new(
            nameof(WorkspaceConfigurationOptions), nameof(DisableCompilationTrackerWeakCompilationReferences), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.DisableCompilationTrackerWeakCompilationReferences"));

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            DisableRecoverableTrees,
            DisableProjectCacheService,
            DisableReferenceManagerWeakRuntimeReferences,
            DisableCompilationTrackerWeakCompilationReferences);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceConfigurationOptions()
        {
        }
    }
}
