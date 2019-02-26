// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
