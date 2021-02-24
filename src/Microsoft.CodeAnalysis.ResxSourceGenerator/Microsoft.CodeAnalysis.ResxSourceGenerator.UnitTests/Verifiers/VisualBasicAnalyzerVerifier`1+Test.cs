// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Microsoft.CodeAnalysis.ResxSourceGenerator.Test
{
    public static partial class VisualBasicAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public class Test : VisualBasicAnalyzerTest<TAnalyzer, XUnitVerifier>
        {
            public Test()
            {
            }
        }
    }
}
