// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.IntroduceVariable;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntroduceVariable;

[Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)]
public sealed partial class IntroduceLocalForExpressionTests : AbstractCSharpCodeActionTest_NoEditor
{
    private static readonly CodeStyleOption2<bool> onWithInfo = new(true, NotificationOption2.Suggestion);
    private static readonly CodeStyleOption2<bool> offWithInfo = new(false, NotificationOption2.Suggestion);

    private OptionsCollection ImplicitTypeEverywhere()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    private OptionsCollection ImplicitTypeForIntrinsics()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    private OptionsCollection ImplicitTypeForApparent()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
        };

    private OptionsCollection ImplicitTypeForApparentAndBuiltIn()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpIntroduceLocalForExpressionCodeRefactoringProvider();

    [Fact]
    public Task IntroduceLocal_NoSemicolon()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    new DateTime()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime {|Rename:dateTime|} = new DateTime();
                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_NoSemicolon_BlankLineAfter()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    new DateTime()[||]

                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime {|Rename:dateTime|} = new DateTime();

                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_NoSemicolon_SelectExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [|new DateTime()|]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime {|Rename:dateTime|} = new DateTime();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task IntroduceLocal_Inside_Expression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    new TimeSpan() +[||] new TimeSpan();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    TimeSpan {|Rename:timeSpan|} = new TimeSpan() + new TimeSpan();
                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_Semicolon()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    new DateTime();[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime {|Rename:dateTime|} = new DateTime();
                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_Semicolon_BlankLineAfter()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    new DateTime();[||]

                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime {|Rename:dateTime|} = new DateTime();

                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_Semicolon_SelectExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [|new DateTime()|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime {|Rename:dateTime|} = new DateTime();
                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_Semicolon_SelectStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [|new DateTime();|]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime {|Rename:dateTime|} = new DateTime();
                }
            }
            """);

    [Fact]
    public Task MissingOnAssignmentExpressionStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    int a = 42;
                    [||]a = 42;
                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_Space()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    new DateTime(); [||]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime {|Rename:dateTime|} = new DateTime(); 
                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_LeadingTrivia()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    // Comment
                    new DateTime();[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    // Comment
                    DateTime {|Rename:dateTime|} = new DateTime();
                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_PreferVar()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    new DateTime();[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    var {|Rename:dateTime|} = new DateTime();
                }
            }
            """, new(options: new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.VarElsewhere, CodeStyleOption2.TrueWithSuggestionEnforcement },
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOption2.TrueWithSuggestionEnforcement },
            }));

    [Fact]
    public Task MissingOnVoidCall()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Console.WriteLine();[||]
                }
            }
            """);

    [Fact]
    public Task MissingOnDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var v = new DateTime()[||]
                }
            }
            """);

    [Fact]
    public Task IntroduceLocal_ArithmeticExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    1 + 1[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    int {|Rename:v|} = 1 + 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction1_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    (string someString, int someInt) = X();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction1_B()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    (string someString, int someInt) {|Rename:value|} = X();
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction1_C()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    var (someString, someInt) = X();
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction2_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X();[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    (string someString, int someInt) = X();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction2_B()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X();[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    (string someString, int someInt) {|Rename:value|} = X();
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction2_C()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X();[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    var (someString, someInt) = X();
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction3_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X()[||]

                    string someString;
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    (string someString1, int someInt) = X();

                    string someString;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction3_B()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X()[||]

                    string someString;
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    (string someString, int someInt) {|Rename:value|} = X();

                    string someString;
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction3_C()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X()[||]

                    string someString;
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    var (someString1, someInt) = X();

                    string someString;
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction4_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                ValueTuple<string, int> X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                ValueTuple<string, int> X() => default;

                void M()
                {
                    (string item1, int item2) = X();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction4_B()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                ValueTuple<string, int> X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                ValueTuple<string, int> X() => default;

                void M()
                {
                    (string, int) {|Rename:value|} = X();
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction4_C()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                ValueTuple<string, int> X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                ValueTuple<string, int> X() => default;

                void M()
                {
                    var (item1, item2) = X();
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction5_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string, int) X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string, int) X() => default;

                void M()
                {
                    (string item1, int item2) = X();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction5_B()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string, int) X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string, int) X() => default;

                void M()
                {
                    (string, int) {|Rename:value|} = X();
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction5_C()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string, int) X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string, int) X() => default;

                void M()
                {
                    var (item1, item2) = X();
                }
            }
            """, new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction_ImplicitTypeForIntrinsics1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    var (someString, someInt) = X();
                }
            }
            """, new(options: ImplicitTypeForIntrinsics()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction_ImplicitTypeForIntrinsics2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, C c) X() => default;

                void M()
                {
                    // don't use `var (...)` here as not all the individual types will be 'var'
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, C c) X() => default;

                void M()
                {
                    // don't use `var (...)` here as not all the individual types will be 'var'
                    (var someString, C c) = X();
                }
            }
            """, new(options: ImplicitTypeForIntrinsics()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction_ImplicitTypeWhenApparent1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    X()[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                (string someString, int someInt) X() => default;

                void M()
                {
                    (string someString, int someInt) = X();
                }
            }
            """, new(options: ImplicitTypeForApparent()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction_ImplicitTypeWhenApparent2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    // literal is not apparent (it is builtin). default(...) is both apparent
                    (someString: "", someC: default(C))[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    // literal is not apparent (it is builtin). default(...) is both apparent
                    (string someString, C someC) = (someString: "", someC: default(C));
                }
            }
            """, new(options: ImplicitTypeForApparent()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39537")]
    public Task IntroduceDeconstruction_ImplicitTypeWhenApparentAndBuiltIn1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    // literal is is builtin, as is default(...)
                    (someString: "", someC: default(C))[||]
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    // literal is is builtin, as is default(...)
                    var (someString, someC) = (someString: "", someC: default(C));
                }
            }
            """, new(options: ImplicitTypeForApparentAndBuiltIn()));
}
