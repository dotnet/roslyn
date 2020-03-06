// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpAvoidOptSuffixForNullableEnableCode,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class AvoidOptSuffixForNullableEnableCodeTests
    {
        [Fact]
        public async Task RS0037_CSharp8_NullableEnabledCode_Diagnostic()
        {
            await new VerifyCS.Test
            {
                TestCode =
                        @"
#nullable enable

public class Class1
{
    private Class1? _instanceOpt;

    public void Method1(string? sOpt)
    {
    }
}",
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [Fact]
        public async Task RS0037_CSharp8_NonNullableEnabledCode_NoDiagnostic()
        {
            await new VerifyCS.Test
            {
                TestCode =
                        @"
public class Class1
{
    private Class1? _instanceOpt;

    public void Method1(string? sOpt)
    {
    }
}",
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [Fact]
        public async Task RS0037_PriorToCSharp8_NoDiagnostic()
        {
            await new VerifyCS.Test
            {
                TestCode =
                        @"
public class Class1
{
    private Class1 _instanceOpt;

    public void Method1(string sOpt)
    {
    }
}",
                LanguageVersion = LanguageVersion.CSharp7_3
            }.RunAsync();
        }
    }
}
