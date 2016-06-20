// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.CSharp.Analyzers;
using Roslyn.Diagnostics.VisualBasic.Analyzers;
using Test.Utilities;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class SymbolDeclaredEventMustBeGeneratedForSourceSymbolsTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicSymbolDeclaredEventAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpSymbolDeclaredEventAnalyzer();
        }
    }
}