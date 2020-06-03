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
        string? [|localOpt|] = null, [|otherLocalOpt|] = null;

        System.Console.WriteLine(""{0}, {1}, {2}, {3}, {4}"", _instanceOpt, otherInstanceOpt, sOpt, localOpt, otherLocalOpt);
    }
}",
                FixedCode = @"
#nullable enable

public class Class1
{
    private Class1? _instance, otherInstance;

    public void Method1(string? s)
    {
        string? local = null, otherLocal = null;

        System.Console.WriteLine(""{0}, {1}, {2}, {3}, {4}"", _instance, otherInstance, s, local, otherLocal);
    }
}",

            }.RunAsync();
        }

        [Fact]
        public async Task RS0046_CSharp8_NullableEnabledCodeNonNullableType_NoDiagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode = @"
#nullable enable

public class Class1
{
    private Class1 _instanceOpt = new Class1(), otherInstanceOpt = new Class1();

    public void Method1(string sOpt)
    {
        string localOpt, otherLocalOpt;
    }
}",

            }.RunAsync();
        }

        [Fact]
        public async Task RS0046_CSharp8_NullableEnabledCodeValueType_Diagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode = @"
#nullable enable

public enum MyEnum { A, }

public class Class1
{
    private MyEnum? [|_instanceOpt|], [|otherInstanceOpt|];

    public void Method1(MyEnum? [|eOpt|])
    {
        MyEnum? [|localOpt|] = null, [|otherLocalOpt|] = null;

        System.Console.WriteLine(""{0}, {1}, {2}, {3}, {4}"", _instanceOpt, otherInstanceOpt, eOpt, localOpt, otherLocalOpt);
    }
}",
                FixedCode = @"
#nullable enable

public enum MyEnum { A, }

public class Class1
{
    private MyEnum? _instance, otherInstance;

    public void Method1(MyEnum? e)
    {
        MyEnum? local = null, otherLocal = null;

        System.Console.WriteLine(""{0}, {1}, {2}, {3}, {4}"", _instance, otherInstance, e, local, otherLocal);
    }
}",

            }.RunAsync();
        }

        [Fact]
        public async Task RS0046_CSharp8_NullableEnabledCodeNonNullableValueType_NoDiagnostic()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode = @"
#nullable enable

public enum MyEnum { A, }

public class Class1
{
    private MyEnum _instanceOpt = MyEnum.A, otherInstanceOpt = MyEnum.A;

    public void Method1(MyEnum eOpt)
    {
        MyEnum localOpt, otherLocalOpt;
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

        [Fact(Skip = "https://github.com/dotnet/roslyn-analyzers/issues/3707")]
        public async Task RS0046_CSharp8_VariableWithoutOptAlreadyExists_DiagnosticButNoCodeFix()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode = @"
#nullable enable

public class Class1
{
    private Class1? [|_instanceOpt|], _instance;

    public void Method1(string? [|sOpt|], string? s)
    {
        string? [|localOpt|], local;
    }
}",
                FixedCode = @"
#nullable enable

public class Class1
{
    private Class1? [|_instanceOpt|], _instance;

    public void Method1(string? [|sOpt|], string? s)
    {
        string? [|localOpt|], local;
    }
}",
            }.RunAsync();
        }

        [Fact]
        public async Task RS0046_CSharp8_UnknownType_DiagnosticAndCodeFix()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp8,
                TestCode = @"
#nullable enable

public class Class1
{
    private {|CS0246:Class2|}? [|_instanceOpt|];

    public void Method1({|CS0246:Class2|}? [|sOpt|])
    {
        {|CS0246:Class2|}? [|localOpt|];
    }
}",
                FixedCode = @"
#nullable enable

public class Class1
{
    private {|CS0246:Class2|}? _instance;

    public void Method1({|CS0246:Class2|}? s)
    {
        {|CS0246:Class2|}? local;
    }
}",
            }.RunAsync();
        }
    }
}
