// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis
{
    [ExportOptionProvider, Shared]
    internal class WorkspaceConfigurationOptions : IOptionProvider
    {
        public static readonly Option<bool> DisableRecoverableTrees = new(
            nameof(WorkspaceConfigurationOptions), nameof(DisableRecoverableTrees), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.DisableRecoverableTrees"));

        public static readonly Option<bool> DisableBranchIds = new(
            nameof(WorkspaceConfigurationOptions), nameof(DisableBranchIds), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.DisableBranchIds"));

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            DisableRecoverableTrees,
            DisableBranchIds);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceConfigurationOptions()
        {
        }
    }
}
