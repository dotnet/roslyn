// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.InlineDeclaration;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitType;
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

    [Fact]
    public async Task InlineVariable1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task InlineInNestedCall()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task InlineVariableWithConstructor1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task InlineVariableMissingWithIndexer1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task InlineVariableIntoFirstOut1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task InlineVariableIntoFirstOut2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestMissingInCSharp6()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task InlineVariablePreferVar1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task InlineVariablePreferVarExceptForPredefinedTypes1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestAvailableWhenWrittenAfter1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestMissingWhenWrittenBetween1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingWhenReadBetween1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingWithComplexInitializer()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestAvailableInOuterScopeIfNotWrittenOutside()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingIfWrittenAfterInOuterScope()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingIfWrittenBetweenInOuterScope()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingInNonOut()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingInField()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingInField2()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingInNonLocalStatement()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMissingInEmbeddedStatementWithWriteAfterwards()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestInEmbeddedStatement()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestAvailableInNestedBlock()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestOverloadResolutionDoNotUseVar1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestOverloadResolutionDoNotUseVar2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestGenericInferenceDoNotUseVar3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments4()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments5()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments6()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments7()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments8()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestComments9()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15994")]
    public async Task TestCommentsTrivia1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15994")]
    public async Task TestCommentsTrivia2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15336")]
    public async Task TestNotMissingIfCapturedInLambdaAndNotUsedAfterwards()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15336")]
    public async Task TestMissingIfCapturedInLambdaAndUsedAfterwards()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15408")]
    public async Task TestDataFlow1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15408")]
    public async Task TestDataFlow2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16028")]
    public async Task TestExpressionTree1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16198")]
    public async Task TestIndentation1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public async Task TestNotInLoops1()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public async Task TestNotInLoops2()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public async Task TestNotInLoops3()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public async Task TestNotInLoops4()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestNotInUsing()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestNotInExceptionFilter()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestNotInShortCircuitExpression1()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestNotInShortCircuitExpression2()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestNotInFixed()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public async Task TestInLoops1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public async Task TestInLoops2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public async Task TestInLoops3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17624")]
    public async Task TestInLoops4()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestInUsing()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestInExceptionFilter()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestInShortCircuitExpression1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestInShortCircuitExpression2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18076")]
    public async Task TestInFixed()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17743")]
    public async Task TestInLocalFunction1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestInLocalFunction2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16676")]
    public async Task TestMultipleDeclarationStatementsOnSameLine1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16676")]
    public async Task TestMultipleDeclarationStatementsOnSameLine2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16676")]
    public async Task TestMultipleDeclarationStatementsOnSameLine3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestMissingOnUnderscore()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18668")]
    public async Task TestDefiniteAssignmentIssueWithVar()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18668")]
    public async Task TestDefiniteAssignmentIssueWithNonVar()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public async Task TestMissingOnCrossFunction1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public async Task TestMissingOnCrossFunction2()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public async Task TestMissingOnCrossFunction3()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public async Task TestMissingOnCrossFunction4()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestDefiniteAssignment1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestDefiniteAssignment2()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Theory]
    [InlineData("c && M(out x)", "c && M(out bool x)")]
    [InlineData("false || M(out x)", "false || M(out bool x)")]
    [InlineData("M(out x) || M(out x)", "M(out bool x) || M(out x)")]
    public async Task TestDefiniteAssignment3(string input, string output)
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task InlineVariable_NullableEnable()
    {
        await TestInRegularAndScript1Async("""
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
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/44429")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/74736")]
    public async Task TopLevelStatement()
    {
        await TestAsync("""
            [|int|] i;
            if (int.TryParse(v, out i))
            {
            }
            """, """
            if (int.TryParse(v, out int i))
            {
            }
            """, CSharpParseOptions.Default);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47041")]
    public async Task CollectionInitializer()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22881")]
    public async Task PriorRegionClose()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInSwitchSection()
    {
        await TestInRegularAndScript1Async(
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
    }
}
