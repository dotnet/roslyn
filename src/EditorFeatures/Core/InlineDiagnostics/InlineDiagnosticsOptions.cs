﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics
{
    internal sealed class InlineDiagnosticsOptions
    {
        public static readonly PerLanguageOption2<bool> EnableInlineDiagnostics =
            new("InlineDiagnosticsOptions_EnableInlineDiagnostics",
                defaultValue: false);

        public static readonly PerLanguageOption2<InlineDiagnosticsLocations> Location =
            new("InlineDiagnosticsOptions_Location",
                defaultValue: InlineDiagnosticsLocations.PlacedAtEndOfCode);
    }
}
