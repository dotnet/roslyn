// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
    public class ParameterWrappingTests : AbstractWrappingTests
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpWrappingCodeRefactoringProvider();

        [Fact]
        public async Task TestMissingWithSyntaxError()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]int i, int j {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithSelection()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([|int|] i, int j) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingInBody()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo(int i, int j) {[||]
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingInAttributes()
        {
            await TestMissingAsync(
                """
                class C {
                    [||][Attr]
                    void Goo(int i, int j) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithOpenTokenTrailingComment()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]/**/int i, int j) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithItemLeadingComment()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]
                        /**/int i, int j) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithItemTrailingComment()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]
                        int i/**/, int j) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithCommaTrailingComment()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]
                        int i,/**/int j) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithLastItemTrailingComment()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]
                        int i, int j/**/
                        ) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithCloseTokenLeadingComment()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]
                        int i, int j
                        /**/) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestWithOpenTokenLeadingComment()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestWithCloseTokenTrailingComment()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestMissingWithSingleParameter()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]int i) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithMultiLineParameter()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo([||]int i, int j =
                        initializer) {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInHeader1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInHeader2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInHeader3()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInHeader4()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestTwoParamWrappingCases()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task TestThreeParamWrappingCases()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task Test_AllOptions_NoInitialMatches()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task Test_LongWrapping_ShortIds()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task Test_LongWrapping_VariadicLengthIds()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task Test_DoNotOfferLongWrappingOptionThatAlreadyAppeared()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task Test_LongWrapping_VariadicLengthIds2()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task Test_DoNotOfferExistingOption1()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task Test_DoNotOfferExistingOption2()
        {
            await TestAllWrappingCasesAsync(
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
        }

        [Fact]
        public async Task TestInConstructor()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38986")]
        public async Task TestInConstructorWithSyntaxErrorAfter()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInIndexer()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInOperator()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInDelegate()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInParenthesizedLambda()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInParenthesizedLambda2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestNotOnSimpleLambda()
        {
            await TestMissingAsync(
                """
                class C {
                    void Goo()
                    {
                        var v = [||]c => {
                        };
                    }
                }
                """);
        }

        [Fact]
        public async Task TestLocalFunction()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestRecord_Semicolon()
        {
            await TestInRegularAndScript1Async(
"record R([||]int I, string S);",
"""
record R(int I,
         string S);
""");
        }

        [Fact]
        public async Task TestClass_Semicolon()
        {
            await TestInRegularAndScript1Async(
"class R([||]int I, string S);",
"""
class R(int I,
        string S);
""");
        }

        [Fact]
        public async Task TestInterface_Semicolon()
        {
            await TestInRegularAndScript1Async(
"interface R([||]int I, string S);",
"""
interface R(int I,
            string S);
""");
        }

        [Fact]
        public async Task TestRecord_Braces()
        {
            await TestInRegularAndScript1Async(
"record R([||]int I, string S) { }",
"""
record R(int I,
         string S) { }
""");
        }

        [Fact]
        public async Task TestClass_Braces()
        {
            await TestInRegularAndScript1Async(
"class R([||]int I, string S) { }",
"""
class R(int I,
        string S) { }
""");
        }

        [Fact]
        public async Task TestInterface_Braces()
        {
            await TestInRegularAndScript1Async(
"interface R([||]int I, string S) { }",
"""
interface R(int I,
            string S) { }
""");
        }

        [Fact]
        public async Task TestRecordStruct_Semicolon()
        {
            await TestInRegularAndScript1Async(
"record struct R([||]int I, string S);",
"""
record struct R(int I,
                string S);
""", new TestParameters(TestOptions.RegularPreview));
        }

        [Fact]
        public async Task TestStruct_Semicolon()
        {
            await TestInRegularAndScript1Async(
"struct R([||]int I, string S);",
"""
struct R(int I,
         string S);
""", new TestParameters(TestOptions.RegularPreview));
        }

        [Fact]
        public async Task TestRecordStruct_Braces()
        {
            await TestInRegularAndScript1Async(
"record struct R([||]int I, string S) { }",
"""
record struct R(int I,
                string S) { }
""", new TestParameters(TestOptions.RegularPreview));
        }

        [Fact]
        public async Task TestStruct_Braces()
        {
            await TestInRegularAndScript1Async(
"struct R([||]int I, string S) { }",
"""
struct R(int I,
         string S) { }
""", new TestParameters(TestOptions.RegularPreview));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61362")]
        public async Task TestWithMissingParameterList()
        {
            await TestMissingAsync(
                """
                class C {
                    public void UpsertRecord<T>[||]
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
        public async Task TestWithMissingStartToken1()
        {
            await TestMissingAsync(
                """
                class C {
                    public void UpsertRecord<T>[||])
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
        public async Task TestWithMissingStartToken2()
        {
            await TestMissingAsync(
                """
                class C {
                    public void UpsertRecord<T>[||] int i, int j)
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
        public async Task TestWithMissingEndToken1()
        {
            await TestMissingAsync(
                """
                class C {
                    public void UpsertRecord<T>([||]
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
        public async Task TestWithMissingEndToken2()
        {
            await TestMissingAsync(
                """
                class C {
                    public void UpsertRecord<T>([||]int i, int j
                }
                """);
        }
    }
}
