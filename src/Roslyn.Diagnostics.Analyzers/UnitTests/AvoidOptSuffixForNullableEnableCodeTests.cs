// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.AvoidOptSuffixForNullableEnableCode,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class AvoidOptSuffixForNullableEnableCodeTests
    {
        [Fact]
        public async Task RS0042_CSharp8_NullableEnabledCode_Diagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode =
                        @"
#nullable enable

public class Class1
{
    private Class1? _instanceOpt; // RS0042

    public void Method1(string? sOpt) // RS0042
    {
    }
}",
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic().WithSpan(6, 21, 6, 33),
                    VerifyCS.Diagnostic().WithSpan(8, 33, 8, 37),
                }
            }.RunAsync();
        }

        [Fact]
        public async Task RS0042_CSharp8_NonNullableEnabledCode_NoDiagnostic()
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
        public async Task RS0042_CSharp8_NullableDisabledCode_NoDiagnostic()
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
        public async Task RS0042_PriorToCSharp8_NoDiagnostic()
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
