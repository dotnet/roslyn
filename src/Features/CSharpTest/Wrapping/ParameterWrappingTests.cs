// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping;

[Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
public sealed class ParameterWrappingTests : AbstractWrappingTests
{
    [Fact]
    public Task TestMissingWithSyntaxError()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]int i, int j {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithSelection()
        => TestMissingAsync(
            """
            class C {
                void Goo([|int|] i, int j) {
                }
            }
            """);

    [Fact]
    public Task TestMissingInBody()
        => TestMissingAsync(
            """
            class C {
                void Goo(int i, int j) {[||]
                }
            }
            """);

    [Fact]
    public Task TestMissingInAttributes()
        => TestMissingAsync(
            """
            class C {
                [||][Attr]
                void Goo(int i, int j) {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithOpenTokenTrailingComment()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]/**/int i, int j) {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithItemLeadingComment()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]
                    /**/int i, int j) {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithItemTrailingComment()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]
                    int i/**/, int j) {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithCommaTrailingComment()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]
                    int i,/**/int j) {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithLastItemTrailingComment()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]
                    int i, int j/**/
                    ) {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithCloseTokenLeadingComment()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]
                    int i, int j
                    /**/) {
                }
            }
            """);

    [Fact]
    public Task TestWithOpenTokenLeadingComment()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo/**/([||]int i, int j) {
                }
            }
            """,

            """
            class C {
                void Goo/**/(int i,
                             int j) {
                }
            }
            """);

    [Fact]
    public Task TestWithCloseTokenTrailingComment()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo([||]int i, int j)/**/ {
                }
            }
            """,

            """
            class C {
                void Goo(int i,
                         int j)/**/ {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithSingleParameter()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]int i) {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithMultiLineParameter()
        => TestMissingAsync(
            """
            class C {
                void Goo([||]int i, int j =
                    initializer) {
                }
            }
            """);

    [Fact]
    public Task TestInHeader1()
        => TestInRegularAndScriptAsync(
            """
            class C {
                [||]void Goo(int i, int j) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                         int j) {
                }
            }
            """);

    [Fact]
    public Task TestInHeader2()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void [||]Goo(int i, int j) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                         int j) {
                }
            }
            """);

    [Fact]
    public Task TestInHeader3()
        => TestInRegularAndScriptAsync(
            """
            class C {
                [||]public void Goo(int i, int j) {
                }
            }
            """,
            """
            class C {
                public void Goo(int i,
                                int j) {
                }
            }
            """);

    [Fact]
    public Task TestInHeader4()
        => TestInRegularAndScriptAsync(
            """
            class C {
                public void Goo(int i, int j)[||] {
                }
            }
            """,
            """
            class C {
                public void Goo(int i,
                                int j) {
                }
            }
            """);

    [Fact]
    public Task TestTwoParamWrappingCases()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]int i, int j) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                         int j) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i,
                    int j) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                    int j) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int j) {
                }
            }
            """);

    [Fact]
    public Task TestThreeParamWrappingCases()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]int i, int j, int k) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                         int j,
                         int k) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i,
                    int j,
                    int k) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                    int j,
                    int k) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int j, int k) {
                }
            }
            """);

    [Fact]
    public Task Test_AllOptions_NoInitialMatches()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]
                    int i,
                        int j,
                            int k) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                         int j,
                         int k) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i,
                    int j,
                    int k) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                    int j,
                    int k) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int j, int k) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int j, int k) {
                }
            }
            """);

    [Fact]
    public Task Test_LongWrapping_ShortIds()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]
                    int i, int j, int k, int l, int m,
                    int n) {
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Goo(int i,
                         int j,
                         int k,
                         int l,
                         int m,
                         int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i,
                    int j,
                    int k,
                    int l,
                    int m,
                    int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                    int j,
                    int k,
                    int l,
                    int m,
                    int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int j, int k, int l, int m, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int j, int k, int l, int m, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int j,
                         int k, int l,
                         int m, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int j, int k,
                    int l, int m, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int j,
                    int k, int l, int m,
                    int n) {
                }
            }
            """);

    [Fact]
    public Task Test_LongWrapping_VariadicLengthIds()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]
                    int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm,
                    int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Goo(int i,
                         int jj,
                         int kkkk,
                         int llllllll,
                         int mmmmmmmmmmmmmmmm,
                         int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i,
                    int jj,
                    int kkkk,
                    int llllllll,
                    int mmmmmmmmmmmmmmmm,
                    int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                    int jj,
                    int kkkk,
                    int llllllll,
                    int mmmmmmmmmmmmmmmm,
                    int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm, int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm, int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj,
                         int kkkk,
                         int llllllll,
                         int mmmmmmmmmmmmmmmm,
                         int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int jj,
                    int kkkk, int llllllll,
                    int mmmmmmmmmmmmmmmm,
                    int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj,
                    int kkkk, int llllllll,
                    int mmmmmmmmmmmmmmmm,
                    int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferLongWrappingOptionThatAlreadyAppeared()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]
                    int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm,
                    int nnnnn) {
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Goo(int iiiii,
                         int jjjjj,
                         int kkkkk,
                         int lllll,
                         int mmmmm,
                         int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int iiiii,
                    int jjjjj,
                    int kkkkk,
                    int lllll,
                    int mmmmm,
                    int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int iiiii,
                    int jjjjj,
                    int kkkkk,
                    int lllll,
                    int mmmmm,
                    int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int iiiii, int jjjjj,
                    int kkkkk, int lllll,
                    int mmmmm, int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int iiiii,
                    int jjjjj, int kkkkk,
                    int lllll, int mmmmm,
                    int nnnnn) {
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]
                    int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm,
                    int nnnnn) {
                }
            }
            """,
            GetIndentionColumn(20),
            """
            class C {
                void Goo(int iiiii,
                         int jjjjj,
                         int kkkkk,
                         int lllll,
                         int mmmmm,
                         int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int iiiii,
                    int jjjjj,
                    int kkkkk,
                    int lllll,
                    int mmmmm,
                    int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int iiiii,
                    int jjjjj,
                    int kkkkk,
                    int lllll,
                    int mmmmm,
                    int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
                }
            }
            """);

    [Fact]
    public Task Test_LongWrapping_VariadicLengthIds2()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]
                    int i, int jj, int kkkk, int lll, int mm,
                    int n) {
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Goo(int i,
                         int jj,
                         int kkkk,
                         int lll,
                         int mm,
                         int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i,
                    int jj,
                    int kkkk,
                    int lll,
                    int mm,
                    int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                    int jj,
                    int kkkk,
                    int lll,
                    int mm,
                    int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj, int kkkk, int lll, int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int jj, int kkkk, int lll, int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj,
                         int kkkk, int lll,
                         int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int jj,
                    int kkkk, int lll,
                    int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj,
                    int kkkk, int lll,
                    int mm, int n) {
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferExistingOption1()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]int i,
                         int jj,
                         int kkkk,
                         int lll,
                         int mm,
                         int n) {
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Goo(
                    int i,
                    int jj,
                    int kkkk,
                    int lll,
                    int mm,
                    int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                    int jj,
                    int kkkk,
                    int lll,
                    int mm,
                    int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj, int kkkk, int lll, int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int jj, int kkkk, int lll, int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj,
                         int kkkk, int lll,
                         int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int jj,
                    int kkkk, int lll,
                    int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj,
                    int kkkk, int lll,
                    int mm, int n) {
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferExistingOption2()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo([||]
                    int i,
                    int jj,
                    int kkkk,
                    int lll,
                    int mm,
                    int n) {
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Goo(int i,
                         int jj,
                         int kkkk,
                         int lll,
                         int mm,
                         int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i,
                    int jj,
                    int kkkk,
                    int lll,
                    int mm,
                    int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj, int kkkk, int lll, int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int jj, int kkkk, int lll, int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj,
                         int kkkk, int lll,
                         int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(
                    int i, int jj,
                    int kkkk, int lll,
                    int mm, int n) {
                }
            }
            """,
            """
            class C {
                void Goo(int i, int jj,
                    int kkkk, int lll,
                    int mm, int n) {
                }
            }
            """);

    [Fact]
    public Task TestInConstructor()
        => TestInRegularAndScriptAsync(
            """
            class C {
                public [||]C(int i, int j) {
                }
            }
            """,
            """
            class C {
                public C(int i,
                         int j) {
                }
            }
            """);

    [Fact]
    public Task TestInPrimaryConstructor()
        => TestInRegularAndScriptAsync(
            """
            class [||]C(int i, int j) {
            }
            """,
            """
            class C(int i,
                    int j) {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38986")]
    public Task TestInConstructorWithSyntaxErrorAfter()
        => TestInRegularAndScriptAsync(
            """
            class C {
                public [||]C(int i, int j) : base(,) {
                }
            }
            """,
            """
            class C {
                public C(int i,
                         int j) : base(,) {
                }
            }
            """);

    [Fact]
    public Task TestInIndexer()
        => TestInRegularAndScriptAsync(
            """
            class C {
                public int [||]this[int i, int j] => 0;
            }
            """,
            """
            class C {
                public int this[int i,
                                int j] => 0;
            }
            """);

    [Fact]
    public Task TestInOperator()
        => TestInRegularAndScriptAsync(
            """
            class C {
                public shared int operator [||]+(C c1, C c2) => 0;
            }
            """,
            """
            class C {
                public shared int operator +(C c1,
                                             C c2) => 0;
            }
            """);

    [Fact]
    public Task TestInDelegate()
        => TestInRegularAndScriptAsync(
            """
            class C {
                public delegate int [||]D(C c1, C c2);
            }
            """,
            """
            class C {
                public delegate int D(C c1,
                                      C c2);
            }
            """);

    [Fact]
    public Task TestInParenthesizedLambda()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo()
                {
                    var v = ([||]C c, C d) => {
                    };
                }
            }
            """,
            """
            class C {
                void Goo()
                {
                    var v = (C c,
                             C d) => {
                    };
                }
            }
            """);

    [Fact]
    public Task TestInParenthesizedLambda2()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo()
                {
                    var v = ([||]c, d) => {
                    };
                }
            }
            """,
            """
            class C {
                void Goo()
                {
                    var v = (c,
                             d) => {
                    };
                }
            }
            """);

    [Fact]
    public Task TestNotOnSimpleLambda()
        => TestMissingAsync(
            """
            class C {
                void Goo()
                {
                    var v = [||]c => {
                    };
                }
            }
            """);

    [Fact]
    public Task TestLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo()
                {
                    void Local([||]C c, C d) {
                    }
                }
            }
            """,
            """
            class C {
                void Goo()
                {
                    void Local(C c,
                               C d) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestRecord_Semicolon()
        => TestInRegularAndScriptAsync(
"record R([||]int I, string S);",
"""
record R(int I,
         string S);
""");

    [Fact]
    public Task TestClass_Semicolon()
        => TestInRegularAndScriptAsync(
"class R([||]int I, string S);",
"""
class R(int I,
        string S);
""");

    [Fact]
    public Task TestInterface_Semicolon()
        => TestInRegularAndScriptAsync(
"interface R([||]int I, string S);",
"""
interface R(int I,
            string S);
""");

    [Fact]
    public Task TestRecord_Braces()
        => TestInRegularAndScriptAsync(
"record R([||]int I, string S) { }",
"""
record R(int I,
         string S) { }
""");

    [Fact]
    public Task TestClass_Braces()
        => TestInRegularAndScriptAsync(
"class R([||]int I, string S) { }",
"""
class R(int I,
        string S) { }
""");

    [Fact]
    public Task TestInterface_Braces()
        => TestInRegularAndScriptAsync(
"interface R([||]int I, string S) { }",
"""
interface R(int I,
            string S) { }
""");

    [Fact]
    public Task TestRecordStruct_Semicolon()
        => TestInRegularAndScriptAsync(
"record struct R([||]int I, string S);",
"""
record struct R(int I,
                string S);
""", new TestParameters(TestOptions.RegularPreview));

    [Fact]
    public Task TestStruct_Semicolon()
        => TestInRegularAndScriptAsync(
"struct R([||]int I, string S);",
"""
struct R(int I,
         string S);
""", new TestParameters(TestOptions.RegularPreview));

    [Fact]
    public Task TestRecordStruct_Braces()
        => TestInRegularAndScriptAsync(
"record struct R([||]int I, string S) { }",
"""
record struct R(int I,
                string S) { }
""", new TestParameters(TestOptions.RegularPreview));

    [Fact]
    public Task TestStruct_Braces()
        => TestInRegularAndScriptAsync(
"struct R([||]int I, string S) { }",
"""
struct R(int I,
         string S) { }
""", new TestParameters(TestOptions.RegularPreview));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61362")]
    public Task TestWithMissingParameterList()
        => TestMissingAsync(
            """
            class C {
                public void UpsertRecord<T>[||]
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestWithMissingStartToken1()
        => TestMissingAsync(
            """
            class C {
                public void UpsertRecord<T>[||])
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestWithMissingStartToken2()
        => TestMissingAsync(
            """
            class C {
                public void UpsertRecord<T>[||] int i, int j)
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestWithMissingEndToken1()
        => TestMissingAsync(
            """
            class C {
                public void UpsertRecord<T>([||]
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestWithMissingEndToken2()
        => TestMissingAsync(
            """
            class C {
                public void UpsertRecord<T>([||]int i, int j
            }
            """);
}
