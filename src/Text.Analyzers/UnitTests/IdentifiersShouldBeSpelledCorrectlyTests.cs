// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Text.CSharp.Analyzers;
using Text.VisualBasic.Analyzers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    Text.CSharp.Analyzers.CSharpIdentifiersShouldBeSpelledCorrectlyAnalyzer,
    Text.CSharp.Analyzers.CSharpIdentifiersShouldBeSpelledCorrectlyFixer>;
using VerifyVB = Microsoft.CodeAnalysis.VisualBasic.Testing.XUnit.CodeFixVerifier<
    Text.VisualBasic.Analyzers.BasicIdentifiersShouldBeSpelledCorrectlyAnalyzer,
    Text.VisualBasic.Analyzers.BasicIdentifiersShouldBeSpelledCorrectlyFixer>;

namespace Text.Analyzers.UnitTests
{
    public class IdentifiersShouldBeSpelledCorrectlyTests
    {
    }
}