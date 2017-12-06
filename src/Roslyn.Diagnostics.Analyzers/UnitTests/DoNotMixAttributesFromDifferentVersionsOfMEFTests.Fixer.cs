// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.CSharp.Analyzers;
using Roslyn.Diagnostics.VisualBasic.Analyzers;
using Test.Utilities;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class DoNotMixAttributesFromDifferentVersionsOfMEFFixerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new DoNotMixAttributesFromDifferentVersionsOfMEFAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DoNotMixAttributesFromDifferentVersionsOfMEFAnalyzer();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new BasicDoNotMixAttributesFromDifferentVersionsOfMEFFixer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CSharpDoNotMixAttributesFromDifferentVersionsOfMEFFixer();
        }
    }
}