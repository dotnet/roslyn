// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Testing.NUnit
{
    public static class AnalyzerVerifier
    {
        public static AnalyzerVerifier<TAnalyzer> Create<TAnalyzer>()
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            return new AnalyzerVerifier<TAnalyzer>();
        }
    }
}
