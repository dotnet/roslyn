// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics
{
    internal sealed class InlineDiagnosticsOptionsStorage
    {
        public static readonly PerLanguageOption2<bool> EnableInlineDiagnostics =
            new("dotnet_enable_inline_diagnostics",
                defaultValue: false);

        public static readonly PerLanguageOption2<InlineDiagnosticsLocations> Location =
            new("dotnet_inline_diagnostics_location",
                defaultValue: InlineDiagnosticsLocations.PlacedAtEndOfCode, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<InlineDiagnosticsLocations>());
    }
}
