// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalRuntimeDiagnosticOptions
    {
        public const string OptionName = "RuntimeDiagnostic";

        public static readonly Option<bool> Syntax = new Option<bool>(OptionName, "Syntax", defaultValue: false);
        public static readonly Option<bool> Semantic = new Option<bool>(OptionName, "Semantic", defaultValue: false);
    }
}
