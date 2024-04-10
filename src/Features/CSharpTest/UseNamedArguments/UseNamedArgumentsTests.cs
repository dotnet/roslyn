// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UseNamedArguments;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNamedArguments
{
    using VerifyCS = CSharpCodeRefactoringVerifier<CSharpUseNamedArgumentsCodeRefactoringProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
    public class UseNamedArgumentsTests
    {
        private static Task TestMissingInRegularAndScriptAsync(string code)
        {
            return VerifyCS.VerifyRefactoringAsync(code, code);
        }

        private static Task TestWithCSharp7(string initialMarkup, string expectedMarkup)
        {
            return new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                LanguageVersion = LanguageVersion.CSharp7,
            }.RunAsync();
        }

        private static Task TestWithCSharp7_2(string initialMarkup, string expectedMarkup, int index = 0)
        {
            return new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionIndex = index,
                LanguageVersion = LanguageVersion.CSharp7_2,
            }.RunAsync();
        }

        private static Task TestWithCSharp7_3(string initialMarkup, string expectedMarkup)
        {
            return new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                LanguageVersion = LanguageVersion.CSharp7_3,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFirstArgument()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact]
        public async Task TestFirstArgument_CSharp7_2_FirstOption()
        {
            // First option only adds the named argument to the specific parameter you're on.
            await TestWithCSharp7_2(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, 2); }");
        }

        [Fact]
        public async Task TestFirstArgument_CSharp7_2_SecondOption()
        {
            // Second option only adds the named argument to parameter you're on and all trailing parameters.
            await TestWithCSharp7_2(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }",
index: 1);
        }

        [Fact]
        public async Task TestNonFirstArgument()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M(1, [||]2); }",
@"class C { void M(int arg1, int arg2) => M(1, arg2: 2); }");
        }

        [Fact]
        public async Task TestNonFirstArgument_CSharp_7_2()
        {
            // Because we're on the last argument, we should only offer one refactoring to the user.
            var initialMarkup = @"class C { void M(int arg1, int arg2) => M(1, [||]2); }";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = @"class C { void M(int arg1, int arg2) => M(1, arg2: 2); }",
                LanguageVersion = LanguageVersion.CSharp7_2,
                ExactActionSetOffered = [string.Format(FeaturesResources.Add_argument_name_0, "arg2")],
            }.RunAsync();
        }

        [Fact]
        public async Task TestDelegate()
        {
            await TestWithCSharp7(
@"class C { void M(System.Action<int> f) => f([||]1); }",
@"class C { void M(System.Action<int> f) => f(obj: 1); }");
        }

        [Fact]
        public async Task TestConditionalMethod()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => this?.M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => this?.M(arg1: 1, arg2: 2); }");
        }

        [Fact]
        public async Task TestConditionalIndexer()
        {
            await TestWithCSharp7(
@"class C { int? this[int arg1, int arg2] => this?[[||]1, 2]; }",
@"class C { int? this[int arg1, int arg2] => this?[arg1: 1, arg2: 2]; }");
        }

        [Fact]
        public async Task TestThisConstructorInitializer()
        {
            await TestWithCSharp7(
@"class C { C(int arg1, int arg2) {} C() : this([||]1, 2) {} }",
@"class C { C(int arg1, int arg2) {} C() : this(arg1: 1, arg2: 2) {} }");
        }

        [Fact]
        public async Task TestBaseConstructorInitializer()
        {
            await TestWithCSharp7(
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base([||]1, 2) {} }",
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base(arg1: 1, arg2: 2) {} }");
        }

        [Fact]
        public async Task TestConstructor()
        {
            await TestWithCSharp7(
@"class C { C(int arg1, int arg2) { new C([||]1, 2); } }",
@"class C { C(int arg1, int arg2) { new C(arg1: 1, arg2: 2); } }");
        }

        [Fact]
        public async Task TestIndexer()
        {
            await TestWithCSharp7(
@"class C { char M(string arg1) => arg1[[||]0]; }",
@"class C { char M(string arg1) => arg1[index: 0]; }");
        }

        [Fact]
        public async Task TestMissingOnArrayIndexer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { int M(int[] arg1) => arg1[[||]0]; }");
        }

        [Fact]
        public async Task TestMissingOnConditionalArrayIndexer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { int? M(int[] arg1) => arg1?[[||]0]; }");
        }

        [Fact]
        public async Task TestMissingOnEmptyArgumentList()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { void M() => M([||]); }");
        }

        [Fact]
        public async Task TestMissingOnExistingArgumentName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { void M(int arg) => M([||]arg: 1); }");
        }

        [Fact]
        public async Task TestEmptyParams()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1); }");
        }

        [Fact]
        public async Task TestSingleParams()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact]
        public async Task TestNamedParams()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, arg2: new int[0]); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: new int[0]); }");
        }

        [Fact]
        public async Task TestExistingArgumentNames()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M([||]1, arg2: 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact]
        public async Task TestExistingUnorderedArgumentNames()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2, int arg3) => M([||]1, arg3: 3, arg2: 2); }",
@"class C { void M(int arg1, int arg2, int arg3) => M(arg1: 1, arg3: 3, arg2: 2); }");
        }

        [Fact]
        public async Task TestPreserveTrivia()
        {
            await TestWithCSharp7(
                """
                class C { void M(int arg1, ref int arg2) => M(

                    [||]1,

                    ref arg1

                    ); }
                """,
                """
                class C { void M(int arg1, ref int arg2) => M(

                    arg1: 1,

                    arg2: ref arg1

                    ); }
                """);
        }

        [Fact]
        public async Task TestMissingOnNameOf()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { string M() => nameof([||]M); }");
        }

        [Fact]
        public async Task TestAttribute()
        {
            await TestWithCSharp7(
                """
                [C([||]1, 2)]
                class C : System.Attribute { public C(int arg1, int arg2) {} }
                """,
                """
                [C(arg1: 1, arg2: 2)]
                class C : System.Attribute { public C(int arg1, int arg2) {} }
                """);
        }

        [Fact]
        public async Task TestAttributeWithNamedProperties()
        {
            await TestWithCSharp7(
                """
                [C([||]1, P = 2)]
                class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }
                """,
                """
                [C(arg1: 1, P = 2)]
                class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestAvailableOnSelectionOfArgument1()
        {
            await TestWithCSharp7(
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M([|1 + 2|], 2);
                }
                """,
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M(arg1: 1 + 2, arg2: 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestAvailableOnFirstTokenOfArgument1()
        {
            await TestWithCSharp7(
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M([||]1 + 2, 2);
                }
                """,
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M(arg1: 1 + 2, arg2: 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestAvailableOnFirstTokenOfArgument2()
        {
            await TestWithCSharp7(
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M(1[||] + 2, 2);
                }
                """,
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M(arg1: 1 + 2, arg2: 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestNotMissingWhenInsideSingleLineArgument1()
        {
            await TestWithCSharp7(
                """
                using System;

                class C
                {
                    void M(Action arg1, int arg2) 
                        => M([||]() => { }, 2);
                }
                """,
                """
                using System;

                class C
                {
                    void M(Action arg1, int arg2) 
                        => M(arg1: () => { }, arg2: 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestNotMissingWhenInsideSingleLineArgument2_CSharp7()
        {
            await TestWithCSharp7(
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M(1 [||]+ 2, 2);
                }
                """,
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M(arg1: 1 + 2, arg2: 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestNotMissingWhenInsideSingleLineArgument2()
        {
            await TestWithCSharp7_3(
                """
                class C
                {
                    void M(int arg1, int arg2)
                        => M(1 [||]+ 2, 2);
                }
                """,
                """
                class C
                {
                    void M(int arg1, int arg2)
                        => M(arg1: 1 + 2, 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestNotMissingWhenInsideSingleLineArgument3()
        {
            await TestWithCSharp7(
                """
                using System;

                class C
                {
                    void M(Action arg1, int arg2) 
                        => M(() => { [||] }, 2);
                }
                """,
                """
                using System;

                class C
                {
                    void M(Action arg1, int arg2) 
                        => M(arg1: () => {  }, arg2: 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestNotMissingWhenInsideSingleLineArgument4()
        {
            await TestWithCSharp7(
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M(1 [||]+ 2, 2);
                }
                """,
                """
                class C
                {
                    void M(int arg1, int arg2) 
                        => M(arg1: 1 + 2, arg2: 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestMissingNotOnStartingLineOfArgument1()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                using System;

                class C
                {
                    void M(Action arg1, int arg2) 
                        => M(() => {
                             [||]
                           }, 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
        public async Task TestMissingWithSelection()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                using System;

                class C
                {
                    void M(Action arg1, int arg2) 
                        => M([|{|CS1503:1 + 2|}|], 3);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")]
        public async Task TestCaretPositionAtTheEnd1()
        {
            await TestWithCSharp7(
                """
                class C
                {
                    void M(int arg1) => M(arg1[||]);
                }
                """,
                """
                class C
                {
                    void M(int arg1) => M(arg1: arg1);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")]
        public async Task TestCaretPositionAtTheEnd2()
        {
            await TestWithCSharp7(
                """
                class C
                {
                    void M(int arg1, int arg2) => M(arg1[||], arg2);
                }
                """,
                """
                class C
                {
                    void M(int arg1, int arg2) => M(arg1: arg1, arg2: arg2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19758")]
        public async Task TestOnTuple()
        {
            await VerifyCS.VerifyRefactoringAsync(
                """
                using System.Linq;
                class C
                {
                    void M(int[] arr) => arr.Zip(arr, (p1, p2) => ([||]p1, p2));
                }
                """,
                """
                using System.Linq;
                class C
                {
                    void M(int[] arr) => arr.Zip(arr, resultSelector: (p1, p2) => (p1, p2));
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23269")]
        public async Task TestCharacterEscape1()
        {
            await TestWithCSharp7(
                """
                class C
                {
                    void M(int @default, int @params) => M([||]1, 2);
                }
                """,
                """
                class C
                {
                    void M(int @default, int @params) => M(@default: 1, @params: 2);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23269")]
        public async Task TestCharacterEscape2()
        {
            await TestWithCSharp7(
                """
                [C([||]1, 2)]
                class C : System.Attribute
                {
                    public C(int @default, int @params) {}
                }
                """,
                """
                [C(@default: 1, @params: 2)]
                class C : System.Attribute
                {
                    public C(int @default, int @params) {}
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39852")]
        public async Task TestMissingForImplicitRangeIndexer()
        {
            await TestMissingInRegularAndScriptAsync(
                @"class C { string M(string arg1) => arg1[[||]1..^1]; }" + TestSources.Range + TestSources.Index);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39852")]
        public async Task TestMissingForImplicitIndexIndexer()
        {
            await TestMissingInRegularAndScriptAsync(
                @"class C { string M(string arg1) => {|CS0029:arg1[[||]^1]|}; }" + TestSources.Index);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39852")]
        public async Task TestForRealRangeIndexer()
        {
            await VerifyCS.VerifyRefactoringAsync(
                """
                using System; 
                class C { 
                    int this[Range range] => default; 
                    int M(C arg1) => arg1[[||]1..^1]; 
                }
                """ + TestSources.Range + TestSources.Index,
                """
                using System; 
                class C { 
                    int this[Range range] => default; 
                    int M(C arg1) => arg1[range: 1..^1]; 
                }
                """ + TestSources.Range + TestSources.Index);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39852")]
        public async Task TestForRealIndexIndexer()
        {
            await VerifyCS.VerifyRefactoringAsync(
                """
                using System; 
                class C { 
                    int this[Index index] => default; 
                    int M(C arg1) => arg1[[||]^1]; 
                }
                """ + TestSources.Index,
                """
                using System; 
                class C { 
                    int this[Index index] => default; 
                    int M(C arg1) => arg1[index: ^1]; 
                }
                """ + TestSources.Index);
        }

        [Fact]
        public async Task TestNoTrailingArgumentsToName()
        {
            // Because we're on the last argument that doesn't have a name, we should only offer one refactoring to the user.
            var initialMarkup = @"class C { void M(int arg1, int arg2, int arg3) => M(1, [||]2, arg3: 3); }";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = @"class C { void M(int arg1, int arg2, int arg3) => M(1, arg2: 2, arg3: 3); }",
                ExactActionSetOffered = [string.Format(FeaturesResources.Add_argument_name_0, "arg2")],
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63362")]
        public async Task TestTrivia()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                class C
                {
                    static void F(string x, string y)
                    {
                        F(
                                // TODO: 1
                                nu[||]ll
                                // TODO: 2
                            ,   null
                            );
                    }
                }
                """, """
                class C
                {
                    static void F(string x, string y)
                    {
                        F(
                                // TODO: 1
                                x: null
                                // TODO: 2
                            ,   null
                            );
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63362")]
        public async Task TestTrivia_Attribute()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                [My(
                    // Comment
                    [||]null/*comment2*/,
                    null)]
                class MyAttribute : System.Attribute
                {
                    public MyAttribute(string x, string y) { }
                }
                """, """
                [My(
                    // Comment
                    x: null/*comment2*/,
                    null)]
                class MyAttribute : System.Attribute
                {
                    public MyAttribute(string x, string y) { }
                }
                """);
        }
    }
}
