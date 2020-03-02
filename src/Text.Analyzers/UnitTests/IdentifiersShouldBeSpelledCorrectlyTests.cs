// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Text.CSharp.Analyzers;
using Text.VisualBasic.Analyzers;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Text.CSharp.Analyzers.CSharpIdentifiersShouldBeSpelledCorrectlyAnalyzer,
    Text.CSharp.Analyzers.CSharpIdentifiersShouldBeSpelledCorrectlyFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Text.VisualBasic.Analyzers.BasicIdentifiersShouldBeSpelledCorrectlyAnalyzer,
    Text.VisualBasic.Analyzers.BasicIdentifiersShouldBeSpelledCorrectlyFixer>;

namespace Text.Analyzers.UnitTests
{
    public class IdentifiersShouldBeSpelledCorrectlyTests
    {
    }
}