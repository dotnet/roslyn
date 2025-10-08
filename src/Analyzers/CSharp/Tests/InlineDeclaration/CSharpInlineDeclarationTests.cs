// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.InlineDeclaration;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitOrExplicitType;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineDeclaration;

[Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
public sealed partial class CSharpInlineDeclarationTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpInlineDeclarationDiagnosticAnalyzer(), new CSharpInlineDeclarationCodeFixProvider());

    private readonly CodeStyleOption2<bool> s_offWithInfo = new(false, NotificationOption2.Suggestion);

    // specify all options explicitly to override defaults.
    private OptionsCollection ExplicitTypeEverywhere()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, s_offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, s_offWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, s_offWithInfo },
        };

    [Fact]
    public Task InlineVariable1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (int.TryParse(v, out i))
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (int.TryParse(v, out int i))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task InlineInNestedCall()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (Goo(int.TryParse(v, out i)))
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (Goo(int.TryParse(v, out int i)))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task InlineVariableWithConstructor1()
        => TestInRegularAndScriptAsync(
            """
            class C1
            {
                public C1(int v, out int i) {}

                void M(int v)
                {
                    [|int|] i;
                    if (new C1(v, out i))
                    {
                    }
                }
            }
            """,
            """
            class C1
            {
                public C1(int v, out int i) {}

                void M(int v)
                {
                    if (new C1(v, out int i))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task InlineVariableMissingWithIndexer1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (this[out i])
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task InlineVariableIntoFirstOut1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (int.TryParse(v, out i, out i))
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (int.TryParse(v, out int i, out i))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task InlineVariableIntoFirstOut2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (int.TryParse(v, out i))
                    {
                    }

                    if (int.TryParse(v, out i))
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (int.TryParse(v, out int i))
                    {
                    }

                    if (int.TryParse(v, out i))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingInCSharp6()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (int.TryParse(v, out i))
                    {
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

    [Fact]
    public Task InlineVariablePreferVar1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string v)
                {
                    [|int|] i;
                    if (int.TryParse(v, out i))
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(string v)
                {
                    if (int.TryParse(v, out var i))
                    {
                    }
                }
            }
            """, new TestParameters(options: new UseImplicitTypeTests().ImplicitTypeEverywhere()));

    [Fact]
    public Task InlineVariablePreferVarExceptForPredefinedTypes1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string v)
                {
                    [|int|] i;
                    if (int.TryParse(v, out i))
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(string v)
                {
                    if (int.TryParse(v, out int i))
                    {
                    }
                }
            }
            """, new TestParameters(options: new UseImplicitTypeTests().ImplicitTypeButKeepIntrinsics()));

    [Fact]
    public Task TestAvailableWhenWrittenAfter1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (int.TryParse(v, out i))
                    {
                    }

                    i = 0;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (int.TryParse(v, out int i))
                    {
                    }

                    i = 0;
                }
            }
            """);

    [Fact]
    public Task TestMissingWhenWrittenBetween1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    i = 0;
                    if (int.TryParse(v, out i))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWhenReadBetween1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i = 0;
                    M1(i);
                    if (int.TryParse(v, out i))
                    {
                    }
                }

                void M1(int i)
                {
                }
            }
            """);

    [Fact]
    public Task TestMissingWithComplexInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i = M1();
                    if (int.TryParse(v, out i))
                    {
                    }
                }

                int M1()
                {
                }
            }
            """);

    [Fact]
    public Task TestAvailableInOuterScopeIfNotWrittenOutside()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i = 0;
                    {
                        if (int.TryParse(v, out i))
                        {
                        }

                        i = 1;
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingIfWrittenAfterInOuterScope()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i = 0;
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }

                    i = 1;
                }
            }
            """);

    [Fact]
    public Task TestMissingIfWrittenBetweenInOuterScope()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i = 0;
                    {
                        i = 1;
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingInNonOut()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (int.TryParse(v, i))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingInField()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [|int|] i;

                void M()
                {
                    if (int.TryParse(v, out this.i))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingInField2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [|int|] i;

                void M()
                {
                    if (int.TryParse(v, out i))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingInNonLocalStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    foreach ([|int|] i in e)
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingInEmbeddedStatementWithWriteAfterwards()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    while (true)
                        if (int.TryParse(v, out i))
                        {
                        }

                    i = 1;
                }
            }
            """);

    [Fact]
    public Task TestInEmbeddedStatement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    while (true)
                        if (int.TryParse(v, out i))
                        {
                            i = 1;
                        }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    while (true)
                        if (int.TryParse(v, out int i))
                        {
                            i = 1;
                        }
                }
            }
            """);

    [Fact]
    public Task TestAvailableInNestedBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    while (true)
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    while (true)
                    {
                        if (int.TryParse(v, out int i))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestOverloadResolutionDoNotUseVar1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (M2(out i))
                    {
                    }
                }

                void M2(out int i)
                {
                }

                void M2(out string s)
                {
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (M2(out int i))
                    {
                    }
                }

                void M2(out int i)
                {
                }

                void M2(out string s)
                {
                }
            }
            """, new TestParameters(options: new UseImplicitTypeTests().ImplicitTypeEverywhere()));

    [Fact]
    public Task TestOverloadResolutionDoNotUseVar2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|var|] i = 0;
                    if (M2(out i))
                    {
                    }
                }

                void M2(out int i)
                {
                }

                void M2(out string s)
                {
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (M2(out int i))
                    {
                    }
                }

                void M2(out int i)
                {
                }

                void M2(out string s)
                {
                }
            }
            """, new TestParameters(options: new UseImplicitTypeTests().ImplicitTypeEverywhere()));

    [Fact]
    public Task TestGenericInferenceDoNotUseVar3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i;
                    if (M2(out i))
                    {
                    }
                }

                void M2<T>(out T i)
                {
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (M2(out int i))
                    {
                    }
                }

                void M2<T>(out T i)
                {
                }
            }
            """, new TestParameters(options: new UseImplicitTypeTests().ImplicitTypeEverywhere()));

    [Fact]
    public Task TestComments1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    // prefix comment
                    [|int|] i;
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // prefix comment
                    {
                        if (int.TryParse(v, out int i))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|int|] i; // suffix comment
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // suffix comment
                    {
                        if (int.TryParse(v, out int i))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    // prefix comment
                    [|int|] i; // suffix comment
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // prefix comment
                    // suffix comment
                    {
                        if (int.TryParse(v, out int i))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [|i|] /*suffix*/, j;
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int j;
                    {
                        if (int.TryParse(v, out int i /*suffix*/))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int /*prefix*/ [|i|], j;
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int j;
                    {
                        if (int.TryParse(v, out int /*prefix*/ i))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int /*prefix*/ [|i|] /*suffix*/, j;
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int j;
                    {
                        if (int.TryParse(v, out int /*prefix*/ i /*suffix*/))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments7()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int j, /*prefix*/ [|i|] /*suffix*/;
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int j;
                    {
                        if (int.TryParse(v, out int /*prefix*/ i /*suffix*/))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    // prefix
                    int j, [|i|]; // suffix
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // prefix
                    int j; // suffix
                    {
                        if (int.TryParse(v, out int i))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int /*int comment*/
                        /*prefix*/ [|i|] /*suffix*/,
                        j;
                    {
                        if (int.TryParse(v, out i))
                        {
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int /*int comment*/
                        j;
                    {
                        if (int.TryParse(v, out int /*prefix*/ i /*suffix*/))
                        {
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15994")]
    public Task TestCommentsTrivia1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine("Goo");

                    int [|result|];
                    if (int.TryParse("12", out result))
                    {

                    }
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine("Goo");

                    if (int.TryParse("12", out int result))
                    {

                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15994")]
    public Task TestCommentsTrivia2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine("Goo");





                    // Goo



                    int [|result|];
                    if (int.TryParse("12", out result))
                    {

                    }
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine("Goo");





                    // Goo



                    if (int.TryParse("12", out int result))
                    {

                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15336")]
    public Task TestNotMissingIfCapturedInLambdaAndNotUsedAfterwards()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    string [|s|];  
                    Bar(() => Baz(out s));
                }

                void Baz(out string s) { }

                void Bar(Action a) { }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    Bar(() => Baz(out string s));
                }

                void Baz(out string s) { }

                void Bar(Action a) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15336")]
    public Task TestMissingIfCapturedInLambdaAndUsedAfterwards()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    string [|s|];  
                    Bar(() => Baz(out s));
                    Console.WriteLine(s);
                }

                void Baz(out string s) { }

                void Bar(Action a) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15408")]
    public Task TestDataFlow1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo(string x)
                {
                    object [|s|] = null; 
                    if (x != null || TryBaz(out s))
                    {
                        Console.WriteLine(s); 
                    }
                }

                private bool TryBaz(out object s)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15408")]
    public Task TestDataFlow2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo(string x)
                {
                    object [|s|] = null; 
                    if (x != null && TryBaz(out s))
                    {
                        Console.WriteLine(s); 
                    }
                }

                private bool TryBaz(out object s)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo(string x)
                {
                    if (x != null && TryBaz(out object s))
                    {
                        Console.WriteLine(s);
                    }
                }

                private bool TryBaz(out object s)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16028")]
    public Task TestExpressionTree1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Linq.Expressions;

            class Program
            {
                static void Main(string[] args)
                {
                    int [|result|];
                    Method(() => GetValue(out result));
                }

                public static void GetValue(out int result)
                {
                    result = 0;
                }

                public static void Method(Expression<Action> expression)
                {

                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16198")]
    public Task TestIndentation1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private int Bar()
                {
                    IProjectRuleSnapshot [|unresolvedReferenceSnapshot|] = null;
                    var itemType = GetUnresolvedReferenceItemType(originalItemSpec,
                                                                  updatedUnresolvedSnapshots,
                                                                  catalogs,
                                                                  out unresolvedReferenceSnapshot);
                }
            }
            """,
            """
            using System;

            class C
            {
                private int Bar()
                {
                    var itemType = GetUnresolvedReferenceItemType(originalItemSpec,
                                                                  updatedUnresolvedSnapshots,
                                                                  catalogs,
                                                                  out IProjectRuleSnapshot unresolvedReferenceSnapshot);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public Task TestNotInLoops1()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    do
                    {
                    }
                    while (!TryExtractTokenFromEmail(out token));

                    Console.WriteLine(token == "Test");
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public Task TestNotInLoops2()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    while (!TryExtractTokenFromEmail(out token))
                    {
                    }

                    Console.WriteLine(token == "Test");
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public Task TestNotInLoops3()
        => TestMissingAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    foreach (var v in TryExtractTokenFromEmail(out token))
                    {
                    }

                    Console.WriteLine(token == "Test");
                }

                private static IEnumerable<bool> TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public Task TestNotInLoops4()
        => TestMissingAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    for ( ; TryExtractTokenFromEmail(out token); )
                    {
                    }

                    Console.WriteLine(token == "Test");
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestNotInUsing()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    using (GetDisposableAndValue(out token))
                    {
                    }

                    Console.WriteLine(token);
                }

                private static IDisposable GetDisposableAndValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestNotInExceptionFilter()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    try
                    {
                    }
                    catch when (GetValue(out token))
                    {
                    }

                    Console.WriteLine(token);
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestNotInShortCircuitExpression1()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|] = null;
                    bool condition = false && GetValue(out token);
                    Console.WriteLine(token);
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestNotInShortCircuitExpression2()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    bool condition = false && GetValue(out token);
                    Console.WriteLine(token);
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestNotInFixed()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                static unsafe void Main(string[] args)
                {
                    string [|token|];
                    fixed (int* p = GetValue(out token))
                    {
                    }

                    Console.WriteLine(token);
                }

                private static int[] GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public Task TestInLoops1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    do
                    {
                    }
                    while (!TryExtractTokenFromEmail(out token));
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    do
                    {
                    }
                    while (!TryExtractTokenFromEmail(out string token));
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public Task TestInLoops2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    while (!TryExtractTokenFromEmail(out token))
                    {
                    }
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    while (!TryExtractTokenFromEmail(out string token))
                    {
                    }
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public Task TestInLoops3()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    foreach (var v in TryExtractTokenFromEmail(out token))
                    {
                    }
                }

                private static IEnumerable<bool> TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    foreach (var v in TryExtractTokenFromEmail(out string token))
                    {
                    }
                }

                private static IEnumerable<bool> TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public Task TestInLoops4()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    for ( ; TryExtractTokenFromEmail(out token); )
                    {
                    }
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    for (; TryExtractTokenFromEmail(out string token);)
                    {
                    }
                }

                private static bool TryExtractTokenFromEmail(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestInUsing()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    using (GetDisposableAndValue(out token))
                    {
                    }
                }

                private static IDisposable GetDisposableAndValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    using (GetDisposableAndValue(out string token))
                    {
                    }
                }

                private static IDisposable GetDisposableAndValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestInExceptionFilter()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    try
                    {
                    }
                    catch when (GetValue(out token))
                    {
                    }
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    try
                    {
                    }
                    catch when (GetValue(out string token))
                    {
                    }
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestInShortCircuitExpression1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|] = null;
                    bool condition = false && GetValue(out token);
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    bool condition = false && GetValue(out string token);
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestInShortCircuitExpression2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    bool condition = false && GetValue(out token);
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    bool condition = false && GetValue(out string token);
                }

                private static bool GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public Task TestInFixed()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    string [|token|];
                    fixed (int* p = GetValue(out token))
                    {
                    }
                }

                private static int[] GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    fixed (int* p = GetValue(out string token))
                    {
                    }
                }

                private static int[] GetValue(out string token)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17743")]
    public Task TestInLocalFunction1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Demo
            {
                static void Main()
                {
                    F();
                    void F()
                    {
                        Action f = () =>
                        {
                            Dictionary<int, int> dict = null;
                            int [|x|] = 0;
                            dict?.TryGetValue(0, out x);
                            Console.WriteLine(x);
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task TestInLocalFunction2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Demo
            {
                static void Main()
                {
                    F();
                    void F()
                    {
                        Action f = () =>
                        {
                            Dictionary<int, int> dict = null;
                            int [|x|] = 0;
                            dict.TryGetValue(0, out x);
                            Console.WriteLine(x);
                        };
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Demo
            {
                static void Main()
                {
                    F();
                    void F()
                    {
                        Action f = () =>
                        {
                            Dictionary<int, int> dict = null;
                            dict.TryGetValue(0, out int x);
                            Console.WriteLine(x);
                        };
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16676")]
    public Task TestMultipleDeclarationStatementsOnSameLine1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
                    string a; string [|b|];
                    Method(out a, out b);
                }
            }
            """,
            """
            class C
            {
                void Goo()
                {
                    string a; 
                    Method(out a, out string b);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16676")]
    public Task TestMultipleDeclarationStatementsOnSameLine2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
                    string a; /*leading*/ string [|b|]; // trailing
                    Method(out a, out b);
                }
            }
            """,
            """
            class C
            {
                void Goo()
                {
                    string a; /*leading*/  // trailing
                    Method(out a, out string b);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16676")]
    public Task TestMultipleDeclarationStatementsOnSameLine3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
                    string a;
                    /*leading*/ string [|b|]; // trailing
                    Method(out a, out b);
                }
            }
            """,
            """
            class C
            {
                void Goo()
                {
                    string a;
                    /*leading*/ // trailing
                    Method(out a, out string b);
                }
            }
            """);

    [Fact]
    public Task TestMissingOnUnderscore()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [|int|] _;
                    if (N(out _)
                    {
                        Console.WriteLine(_);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18668")]
    public Task TestDefiniteAssignmentIssueWithVar()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M(bool condition)
                {
                    [|var|] x = 1;
                    var result = condition && int.TryParse("2", out x);
                    Console.WriteLine(x);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18668")]
    public Task TestDefiniteAssignmentIssueWithNonVar()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M(bool condition)
                {
                    [|int|] x = 1;
                    var result = condition && int.TryParse("2", out x);
                    Console.WriteLine(x);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public Task TestMissingOnCrossFunction1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
              static void Main(string[] args)
              {
                Method<string>();
              }

              public static void Method<T>()
              { 
                [|T t|];
                void Local<T>()
                {
                  Out(out t);
                  Console.WriteLine(t);
                }
                Local<int>();
              }

              public static void Out<T>(out T t) => t = default;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public Task TestMissingOnCrossFunction2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
              static void Main(string[] args)
              {
                Method<string>();
              }

              public static void Method<T>()
              { 
                void Local<T>()
                {
                    [|T t|];
                    void InnerLocal<T>()
                    {
                      Out(out t);
                      Console.WriteLine(t);
                    }
                }
                Local<int>();
              }

              public static void Out<T>(out T t) => t = default;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public Task TestMissingOnCrossFunction3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Method<string>();
                }

                public static void Method<T>()
                { 
                    [|T t|];
                    void Local<T>()
                    {
                        { // <-- note this set of added braces
                            Out(out t);
                            Console.WriteLine(t);
                        }
                    }
                    Local<int>();
                }

                public static void Out<T>(out T t) => t = default;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public Task TestMissingOnCrossFunction4()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Method<string>();
                }

                public static void Method<T>()
                {
                    { // <-- note this set of added braces
                        [|T t|];
                        void Local<T>()
                        {
                            { // <-- and my axe
                                Out(out t);
                                Console.WriteLine(t);
                            }
                        }
                        Local<int>();
                    }
                }

                public static void Out<T>(out T t) => t = default;
            }
            """);

    [Fact]
    public Task TestDefiniteAssignment1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static bool M(out bool i) => throw null;

                static void M(bool condition)
                {
                    [|bool|] x = false;
                    if (condition || M(out x))
                    {
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact]
    public Task TestDefiniteAssignment2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static bool M(out bool i) => throw null;
                static bool Use(bool i) => throw null;

                static void M(bool condition)
                {
                    [|bool|] x = false;
                    if (condition || M(out x))
                    {
                        x = Use(x);
                    }
                }
            }
            """);

    [Theory]
    [InlineData("c && M(out x)", "c && M(out bool x)")]
    [InlineData("false || M(out x)", "false || M(out bool x)")]
    [InlineData("M(out x) || M(out x)", "M(out bool x) || M(out x)")]
    public Task TestDefiniteAssignment3(string input, string output)
        => TestInRegularAndScriptAsync(
            $$"""
            using System;

            class C
            {
                static bool M(out bool i) => throw null;
                static bool Use(bool i) => throw null;

                static void M(bool c)
                {
                    [|bool|] x = false;
                    if ({{input}})
                    {
                        Console.WriteLine(x);
                    }
                }
            }
            """,
            $$"""
            using System;

            class C
            {
                static bool M(out bool i) => throw null;
                static bool Use(bool i) => throw null;

                static void M(bool c)
                {
                    if ({{output}})
                    {
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact]
    public Task InlineVariable_NullableEnable()
        => TestInRegularAndScriptAsync("""
            #nullable enable
            class C
            {
                void M(out C c2)
                {
                    [|C|] c;
                    M(out c);
                    c2 = c;
                }
            }
            """, """
            #nullable enable
            class C
            {
                void M(out C c2)
                {
                    M(out C c);
                    c2 = c;
                }
            }
            """);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/44429")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/74736")]
    public Task TopLevelStatement()
        => TestAsync("""
            [|int|] i;
            if (int.TryParse(v, out i))
            {
            }
            """, """
            if (int.TryParse(v, out int i))
            {
            }
            """, new(CSharpParseOptions.Default));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47041")]
    public Task CollectionInitializer()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private List<Func<string, bool>> _funcs2 = new List<Func<string, bool>>()
                {
                    s => { int [|i|] = 0; return int.TryParse(s, out i); }
                };
            }
            """,
            """
            class C
            {
                private List<Func<string, bool>> _funcs2 = new List<Func<string, bool>>()
                {
                    s => { return int.TryParse(s, out int i); }
                };
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22881")]
    public Task PriorRegionClose()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    #region test

                    int i = 0;

                    #endregion

                    int [|hello|];
                    TestMethod(out hello);
                }

                private void TestMethod(out int hello)
                {
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    #region test

                    int i = 0;

                    #endregion

                    TestMethod(out int hello);
                }

                private void TestMethod(out int hello)
                {
                }
            }
            """);

    [Fact]
    public Task TestInSwitchSection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s:
                            [|int|] i;
                            if (int.TryParse(v, out i))
                            {
                            }
                    }
                }
            }
            """,
            """
            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case string s:
                            if (int.TryParse(v, out int i))
                            {
                            }
                    }
                }
            }
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/35993")]
    public async Task InlineTemporarySpacing(
        bool preferIntrinsicPredefinedTypeKeywordInDeclaration,
        ReportDiagnostic preferIntrinsicPredefinedTypeKeywordInDeclarationDiagnostic,
        bool varForBuiltInTypes,
        ReportDiagnostic varForBuiltInTypesDiagnostic,
        bool ignoreSpacing)
    {
        if (preferIntrinsicPredefinedTypeKeywordInDeclarationDiagnostic == ReportDiagnostic.Default ||
            varForBuiltInTypesDiagnostic == ReportDiagnostic.Default)
        {
            return;
        }

        var expectedType = varForBuiltInTypes ? "var" : preferIntrinsicPredefinedTypeKeywordInDeclaration ? "bool" : "Boolean";
        await TestInRegularAndScriptAsync(
            """
            using System;

            namespace ClassLibrary5
            {
                public class Class1
                {
                    void A()
                    {
                        bool [||]x;
                        var result = B(out x);
                    }

                    object B(out bool x)
                    {
                        x = default;
                        return default;
                    }
                }
            }
            """,
            $$"""
            using System;

            namespace ClassLibrary5
            {
                public class Class1
                {
                    void A()
                    {
                        var result = B(out {{expectedType}} x);
                    }

                    object B(out bool x)
                    {
                        x = default;
                        return default;
                    }
                }
            }
            """, new(options: new(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, new CodeStyleOption2<bool>(preferIntrinsicPredefinedTypeKeywordInDeclaration, new NotificationOption2(preferIntrinsicPredefinedTypeKeywordInDeclarationDiagnostic, false)) },
                { CSharpCodeStyleOptions.VarForBuiltInTypes, new CodeStyleOption2<bool>(varForBuiltInTypes, new NotificationOption2(varForBuiltInTypesDiagnostic, false)) },
                { CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, ignoreSpacing },
            }));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62805")]
    public Task TestDirectiveWithFixAll1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                #region outer
                void M(out int a, out int b)
                {
                    #region inner
                    {|FixAllInDocument:int|} c;
                    int d;
                    M(out c, out d);
                    #endregion
                }
                #endregion
            }
            """,
            """
            class C
            {
                #region outer
                void M(out int a, out int b)
                {
                    #region inner
                    M(out int c, out int d);
                    #endregion
                }
                #endregion
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32427")]
    public Task TestExplicitTypeEverywhere()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Class1<TFirst, TSecond>
            {
                void A(Dictionary<TFirst, TSecond> map, TFirst first)
                {
                    [|TSecond|] x;
                    if (map.TryGetValue(first, out x))
                    {
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;
            
            public class Class1<TFirst, TSecond>
            {
                void A(Dictionary<TFirst, TSecond> map, TFirst first)
                {
                    if (map.TryGetValue(first, out TSecond x))
                    {
                    }
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40650")]
    public Task TestReferencedInSwitchArms1()
        => TestMissingAsync(
            """
            using System.Collections.Generic;

            class C
            {
                private static int Main(string[] args)
                {
                    Dictionary<int, int> dict = new Dictionary<int, int> { /* ... */ };
                    [|int|] price; // IDE0018 
                    bool found = args[0] switch
                    {
                        "First" => dict.TryGetValue(1, out price),
                        "Second" => dict.TryGetValue(2, out price),
                        _ => dict.TryGetValue(3, out price)
                    };

                    return found ? -1 : price;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40650")]
    public Task TestReferencedInSwitchArms2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                private static int Main(string[] args)
                {
                    Dictionary<int, int> dict = new Dictionary<int, int> { /* ... */ };
                    [|int|] price;
                    bool found = args[0] switch
                    {
                        "First" => dict.TryGetValue(1, out price) ? price == 1 : false,
                        _ => false,
                    };

                    return found;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                private static int Main(string[] args)
                {
                    Dictionary<int, int> dict = new Dictionary<int, int> { /* ... */ };
                    bool found = args[0] switch
                    {
                        "First" => dict.TryGetValue(1, out int price) ? price == 1 : false,
                        _ => false,
                    };

                    return found;
                }
            }
            """);
}
