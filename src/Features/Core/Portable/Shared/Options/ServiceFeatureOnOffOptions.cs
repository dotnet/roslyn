// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal static class ServiceFeatureOnOffOptions
    {
        /// <summary>
        /// This option is used by TypeScript.
        /// </summary>
#pragma warning disable RS0030 // Do not used banned APIs - to avoid a binary breaking API change.
        public static readonly PerLanguageOption<bool> RemoveDocumentDiagnosticsOnDocumentClose = new PerLanguageOption<bool>(
            "ServiceFeatureOnOffOptions", "RemoveDocumentDiagnosticsOnDocumentClose", defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RemoveDocumentDiagnosticsOnDocumentClose"));
#pragma warning restore RS0030 // Do not used banned APIs
    }
}
