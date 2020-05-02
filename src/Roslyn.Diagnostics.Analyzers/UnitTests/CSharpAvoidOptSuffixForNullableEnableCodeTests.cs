// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpAvoidOptSuffixForNullableEnableCode,
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpAvoidOptSuffixForNullableEnableCodeCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class CSharpAvoidOptSuffixForNullableEnableCodeTests
    {
        [Fact]
        public async Task RS0046_CSharp8_NullableEnabledCode_Diagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode = @"
#nullable enable

public class Class1
{
    private Class1? [|_instanceOpt|], [|otherInstanceOpt|];

    public void Method1(string? [|sOpt|])
    {
        string? [|localOpt|], [|otherLocalOpt|];
    }
}",
                FixedCode = @"
#nullable enable

public class Class1
{
    private Class1? _instanceOpt, [|otherInstanceOpt|];

    public void Method1(string? [|sOpt|])
    {
        string? [|localOpt|], [|otherLocalOpt|];
    }
}",
                BatchFixedCode = @"
#nullable enable

public class Class1
{
    private Class1? _instance, otherInstance;

    public void Method1(string? s)
    {
        string? other, otherLocalOpt;
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
    private Class1 _instanceOpt, otherInstanceOpt;

    public void Method1(string sOpt)
    {
        string localOpt, otherLocalOpt;
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
    private Class1 _instanceOpt, otherInstanceOpt;

    public void Method1(string sOpt)
    {
        string localOpt, otherLocalOpt;
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
    private Class1 _instanceOpt, otherInstanceOpt;

    public void Method1(string sOpt)
    {
        string localOpt, otherLocalOpt;
    }
}",
            }.RunAsync();
        }
    }
}
