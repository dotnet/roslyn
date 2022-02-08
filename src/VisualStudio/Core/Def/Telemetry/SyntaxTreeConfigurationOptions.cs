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
        public static readonly Option2<bool> DisableRecoverableTrees = new(
            nameof(WorkspaceConfigurationOptions), nameof(DisableRecoverableTrees), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.DisableRecoverableTrees"));

        public static readonly Option2<bool> DisableProjectCacheService = new(
            nameof(WorkspaceConfigurationOptions), nameof(DisableProjectCacheService), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.DisableProjectCacheService"));

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            DisableRecoverableTrees,
            DisableProjectCacheService);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceConfigurationOptions()
        {
        }
    }
}
