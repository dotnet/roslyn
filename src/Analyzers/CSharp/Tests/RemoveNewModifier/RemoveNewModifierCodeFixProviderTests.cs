// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveNewModifier;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveNewModifier;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNewModifier)]
public sealed class RemoveNewModifierCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public RemoveNewModifierCodeFixProviderTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new RemoveNewModifierCodeFixProvider());

    [Theory]
    [InlineData(
        @"public static new void [|Method()|] { }",
        @"public static void [|Method()|] { }")]
    [InlineData(
        "public new int [|Test|];",
        "public int [|Test|];")]
    [InlineData(
        "public new int [|Test|] { get; set; }",
        "public int [|Test|] { get; set; }")]
    [InlineData(
        "public new const int [|test|] = 1;",
        "public const int test = 1;")]
    [InlineData(
        "public new event Action [|Test|];",
        "public event Action Test;")]
    [InlineData(
        "public new int [|this[int p]|] => p;",
        "public int this[int p] => p;")]
    [InlineData(
        "new class [|Test|] { }",
        "class Test { }")]
    [InlineData(
        "new struct [|Test|] { }",
        "struct Test { }")]
    [InlineData(
        "new interface [|Test|] { }",
        "interface Test { }")]
    [InlineData(
        "new delegate [|Test|]()",
        "delegate Test()")]
    [InlineData(
        "new enum [|Test|] { }",
        "enum Test { }")]
    [InlineData(
        "new(int a, int b) [|test|];",
        "(int a, int b) test;")]
    public Task TestRemoveNewModifierFromMembersWithRegularFormatting(string original, string expected)
        => TestRemoveNewModifierCodeFixAsync(original, expected);

    [Theory]
    [InlineData(
        "/* start */ public /* middle */ new /* end */ int [|Test|];",
        "/* start */ public /* middle */ int Test;")]
    [InlineData(
        "/* start */ public /* middle */ new    /* end */ int [|Test|];",
        "/* start */ public /* middle */ int Test;")]
    [InlineData(
        "/* start */ public /* middle */new /* end */ int [|Test|];",
        "/* start */ public /* middle */int Test;")]
    [InlineData(
        "/* start */ public /* middle */ new/* end */ int [|Test|];",
        "/* start */ public /* middle */ int Test;")]
    [InlineData(
        "/* start */ public /* middle */new/* end */ int [|Test|];",
        "/* start */ public /* middle */int Test;")]
    [InlineData(
        "new /* end */ int [|Test|];",
        "int Test;")]
    [InlineData(
        "new     int [|Test|];",
        "int Test;")]
    [InlineData(
        "/* start */ new /* end */ int [|Test|];",
        "/* start */ int [|Test|];")]
    public Task TestRemoveNewFromModifiersWithComplexTrivia(string original, string expected)
        => TestRemoveNewModifierCodeFixAsync(original, expected);

    [Fact]
    public Task TestRemoveNewFromModifiersFixAll()
        => TestInRegularAndScriptAsync("""
            using System;	
            class B	
            {	
                public int ValidNew;	
            }	
            class C : B	
            {	
                public new int ValidNew;	
                public new void {|FixAllInDocument:M|}() { }	
                public new int F;	
                public new event Action E;	
                public new int P { get; }	
                public new int this[int p] => p;	
                new class C2 { }	
                new struct S2 { }	
                new interface I2 { }	
                new delegate void D2();	
                new enum E2 { }	
            }
            """,
            """
            using System;	
            class B	
            {	
                public int ValidNew;	
            }	
            class C : B	
            {	
                public new int ValidNew;	
                public void M() { }	
                public int F;	
                public event Action E;	
                public int P { get; }	
                public int this[int p] => p;	
                class C2 { }	
                struct S2 { }	
                interface I2 { }	
                delegate void D2();	
                enum E2 { }	
            }
            """);

    private Task TestRemoveNewModifierCodeFixAsync(string original, string expected)
    {
        return TestInRegularAndScriptAsync(
            $$"""
            class App
            {
                {{original}}
            }
            """,
            $$"""
            class App
            {
                {{expected}}
            }
            """);
    }
}
