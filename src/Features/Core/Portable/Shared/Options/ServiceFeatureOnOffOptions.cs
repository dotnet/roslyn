// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal static class ServiceFeatureOnOffOptions
    {
        /// <summary>
        /// This option is used by TypeScript.
        /// </summary>
        [Obsolete("Currently used by TypeScript - should move to the new option SolutionCrawlerOptions.BackgroundAnalysisScopeOption")]
        public static readonly PerLanguageOption<bool?> ClosedFileDiagnostic = SolutionCrawlerOptions.ClosedFileDiagnostic;

        /// <summary>
        /// This option is used by TypeScript.
        /// </summary>
        public static readonly PerLanguageOption<bool> RemoveDocumentDiagnosticsOnDocumentClose = new PerLanguageOption<bool>(
            "ServiceFeatureOnOffOptions", "RemoveDocumentDiagnosticsOnDocumentClose", defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RemoveDocumentDiagnosticsOnDocumentClose"));
    }
}
