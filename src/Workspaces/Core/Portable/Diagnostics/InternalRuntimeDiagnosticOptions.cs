// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalRuntimeDiagnosticOptions
    {
        public static readonly Option2<bool> Syntax = new(nameof(InternalRuntimeDiagnosticOptions), nameof(Syntax), defaultValue: false);
        public static readonly Option2<bool> Semantic = new(nameof(InternalRuntimeDiagnosticOptions), nameof(Semantic), defaultValue: false);
        public static readonly Option2<bool> ScriptSemantic = new(nameof(InternalRuntimeDiagnosticOptions), nameof(ScriptSemantic), defaultValue: false);
    }
}
