// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalRuntimeDiagnosticOptions
    {
        public static readonly Option<bool> Syntax = new Option<bool>(nameof(InternalRuntimeDiagnosticOptions), nameof(Syntax), defaultValue: false);
        public static readonly Option<bool> Semantic = new Option<bool>(nameof(InternalRuntimeDiagnosticOptions), nameof(Semantic), defaultValue: false);
        public static readonly Option<bool> ScriptSemantic = new Option<bool>(nameof(InternalRuntimeDiagnosticOptions), nameof(ScriptSemantic), defaultValue: false);
    }
}
