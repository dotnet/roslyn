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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNamedArguments;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpUseNamedArgumentsCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
public sealed class UseNamedArgumentsTests
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
    public Task TestFirstArgument()
        => TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");

    [Fact]
    public Task TestFirstArgument_CSharp7_2_FirstOption()
        => TestWithCSharp7_2(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, 2); }");

    [Fact]
    public Task TestFirstArgument_CSharp7_2_SecondOption()
        => TestWithCSharp7_2(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }",
index: 1);

    [Fact]
    public Task TestNonFirstArgument()
        => TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M(1, [||]2); }",
@"class C { void M(int arg1, int arg2) => M(1, arg2: 2); }");

    [Fact]
    public Task TestNonFirstArgument_CSharp_7_2()
        => new VerifyCS.Test
        {
            TestCode = @"class C { void M(int arg1, int arg2) => M(1, [||]2); }",
            FixedCode = @"class C { void M(int arg1, int arg2) => M(1, arg2: 2); }",
            LanguageVersion = LanguageVersion.CSharp7_2,
            ExactActionSetOffered = [string.Format(FeaturesResources.Add_argument_name_0, "arg2")],
        }.RunAsync();

    [Fact]
    public Task TestDelegate()
        => TestWithCSharp7(
@"class C { void M(System.Action<int> f) => f([||]1); }",
@"class C { void M(System.Action<int> f) => f(obj: 1); }");

    [Fact]
    public Task TestConditionalMethod()
        => TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => this?.M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => this?.M(arg1: 1, arg2: 2); }");

    [Fact]
    public Task TestConditionalIndexer()
        => TestWithCSharp7(
@"class C { int? this[int arg1, int arg2] => this?[[||]1, 2]; }",
@"class C { int? this[int arg1, int arg2] => this?[arg1: 1, arg2: 2]; }");

    [Fact]
    public Task TestThisConstructorInitializer()
        => TestWithCSharp7(
@"class C { C(int arg1, int arg2) {} C() : this([||]1, 2) {} }",
@"class C { C(int arg1, int arg2) {} C() : this(arg1: 1, arg2: 2) {} }");

    [Fact]
    public Task TestBaseConstructorInitializer()
        => TestWithCSharp7(
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base([||]1, 2) {} }",
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base(arg1: 1, arg2: 2) {} }");

    [Fact]
    public Task TestConstructor()
        => TestWithCSharp7(
@"class C { C(int arg1, int arg2) { new C([||]1, 2); } }",
@"class C { C(int arg1, int arg2) { new C(arg1: 1, arg2: 2); } }");

    [Fact]
    public Task TestIndexer()
        => TestWithCSharp7(
@"class C { char M(string arg1) => arg1[[||]0]; }",
@"class C { char M(string arg1) => arg1[index: 0]; }");

    [Fact]
    public Task TestMissingOnArrayIndexer()
        => TestMissingInRegularAndScriptAsync(
@"class C { int M(int[] arg1) => arg1[[||]0]; }");

    [Fact]
    public Task TestMissingOnConditionalArrayIndexer()
        => TestMissingInRegularAndScriptAsync(
@"class C { int? M(int[] arg1) => arg1?[[||]0]; }");

    [Fact]
    public Task TestMissingOnEmptyArgumentList()
        => TestMissingInRegularAndScriptAsync(
@"class C { void M() => M([||]); }");

    [Fact]
    public Task TestMissingOnExistingArgumentName()
        => TestMissingInRegularAndScriptAsync(
@"class C { void M(int arg) => M([||]arg: 1); }");

    [Fact]
    public Task TestEmptyParams()
        => TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1); }");

    [Fact]
    public Task TestSingleParams()
        => TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: 2); }");

    [Fact]
    public Task TestNamedParams()
        => TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, arg2: new int[0]); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: new int[0]); }");

    [Fact]
    public Task TestExistingArgumentNames()
        => TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M([||]1, arg2: 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");

    [Fact]
    public Task TestExistingUnorderedArgumentNames()
        => TestWithCSharp7(
@"class C { void M(int arg1, int arg2, int arg3) => M([||]1, arg3: 3, arg2: 2); }",
@"class C { void M(int arg1, int arg2, int arg3) => M(arg1: 1, arg3: 3, arg2: 2); }");

    [Fact]
    public Task TestPreserveTrivia()
        => TestWithCSharp7(
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

    [Fact]
    public Task TestMissingOnNameOf()
        => TestMissingInRegularAndScriptAsync(
@"class C { string M() => nameof([||]M); }");

    [Fact]
    public Task TestAttribute()
        => TestWithCSharp7(
            """
            [C([||]1, 2)]
            class C : System.Attribute { public C(int arg1, int arg2) {} }
            """,
            """
            [C(arg1: 1, arg2: 2)]
            class C : System.Attribute { public C(int arg1, int arg2) {} }
            """);

    [Fact]
    public Task TestAttributeWithNamedProperties()
        => TestWithCSharp7(
            """
            [C([||]1, P = 2)]
            class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }
            """,
            """
            [C(arg1: 1, P = 2)]
            class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestAvailableOnSelectionOfArgument1()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestAvailableOnFirstTokenOfArgument1()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestAvailableOnFirstTokenOfArgument2()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestNotMissingWhenInsideSingleLineArgument1()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestNotMissingWhenInsideSingleLineArgument2_CSharp7()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestNotMissingWhenInsideSingleLineArgument2()
        => TestWithCSharp7_3(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestNotMissingWhenInsideSingleLineArgument3()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestNotMissingWhenInsideSingleLineArgument4()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestMissingNotOnStartingLineOfArgument1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18848")]
    public Task TestMissingWithSelection()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(Action arg1, int arg2) 
                    => M([|{|CS1503:1 + 2|}|], 3);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")]
    public Task TestCaretPositionAtTheEnd1()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19175")]
    public Task TestCaretPositionAtTheEnd2()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19758")]
    public Task TestOnTuple()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23269")]
    public Task TestCharacterEscape1()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23269")]
    public Task TestCharacterEscape2()
        => TestWithCSharp7(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39852")]
    public Task TestMissingForImplicitRangeIndexer()
        => TestMissingInRegularAndScriptAsync(
            @"class C { string M(string arg1) => arg1[[||]1..^1]; }" + TestSources.Range + TestSources.Index);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39852")]
    public Task TestMissingForImplicitIndexIndexer()
        => TestMissingInRegularAndScriptAsync(
            @"class C { string M(string arg1) => {|CS0029:arg1[[||]^1]|}; }" + TestSources.Index);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39852")]
    public Task TestForRealRangeIndexer()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39852")]
    public Task TestForRealIndexIndexer()
        => VerifyCS.VerifyRefactoringAsync(
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

    [Fact]
    public Task TestNoTrailingArgumentsToName()
        => new VerifyCS.Test
        {
            TestCode = @"class C { void M(int arg1, int arg2, int arg3) => M(1, [||]2, arg3: 3); }",
            FixedCode = @"class C { void M(int arg1, int arg2, int arg3) => M(1, arg2: 2, arg3: 3); }",
            ExactActionSetOffered = [string.Format(FeaturesResources.Add_argument_name_0, "arg2")],
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63362")]
    public Task TestTrivia()
        => VerifyCS.VerifyRefactoringAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63362")]
    public Task TestTrivia_Attribute()
        => VerifyCS.VerifyRefactoringAsync("""
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
