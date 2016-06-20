// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Text.CSharp.Analyzers;
using Text.VisualBasic.Analyzers;

namespace Text.Analyzers.UnitTests
{
    public class IdentifiersShouldBeSpelledCorrectlyTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicIdentifiersShouldBeSpelledCorrectlyAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpIdentifiersShouldBeSpelledCorrectlyAnalyzer();
        }
    }
}