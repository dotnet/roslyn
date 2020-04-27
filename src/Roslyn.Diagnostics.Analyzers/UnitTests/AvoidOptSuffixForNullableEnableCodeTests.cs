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
        public async Task RS0046_CSharp8_NullableEnabledCode_Diagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode =
                        @"
#nullable enable

public class Class1
{
    private Class1? [|_instanceOpt|];

    public void Method1(string? [|sOpt|])
    {
    }
}",
            }.RunAsync();
        }

        [Fact]
        public async Task RS0046_CSharp8_NonNullableEnabledCode_NoDiagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode =
                        @"
public class Class1
{
    private Class1 _instanceOpt;

    public void Method1(string sOpt)
    {
    }
}",
            }.RunAsync();
        }

        [Fact]
        public async Task RS0046_CSharp8_NullableDisabledCode_NoDiagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode =
@"
#nullable disable

public class Class1
{
    private Class1 _instanceOpt;

    public void Method1(string sOpt)
    {
    }
}",
            }.RunAsync();
        }

        [Fact]
        public async Task RS0046_PriorToCSharp8_NoDiagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp7_3,
                TestCode =
                        @"
public class Class1
{
    private Class1 _instanceOpt;

    public void Method1(string sOpt)
    {
    }
}",
            }.RunAsync();
        }
    }
}
