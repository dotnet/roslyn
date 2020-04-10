// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    /// <summary>
    /// Options that aren't persisted. options here will be reset to default on new process.
    /// </summary>
    internal static class RuntimeOptions
    {
        public static readonly Option<bool> BackgroundAnalysisSuspendedInfoBarShown = new Option<bool>(nameof(RuntimeOptions), "FullSolutionAnalysisInfoBarShown", defaultValue: false);
    }
}
