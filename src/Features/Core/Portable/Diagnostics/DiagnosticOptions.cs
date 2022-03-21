// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportSolutionOptionProvider, Shared]
    internal sealed class DiagnosticOptions : IOptionProvider
    {
        private const string FeatureName = "DiagnosticOptions";

        public static readonly Option2<bool> LspPullDiagnosticsFeatureFlag = new(
            FeatureName, nameof(LspPullDiagnosticsFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Lsp.PullDiagnostics"));

        public static readonly Option2<bool> LogTelemetryForBackgroundAnalyzerExecution = new(
            FeatureName, nameof(LogTelemetryForBackgroundAnalyzerExecution), defaultValue: false,
            new FeatureFlagStorageLocation($"Roslyn.LogTelemetryForBackgroundAnalyzerExecution"));

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            LspPullDiagnosticsFeatureFlag,
            LogTelemetryForBackgroundAnalyzerExecution);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticOptions()
        {
        }
    }
}
