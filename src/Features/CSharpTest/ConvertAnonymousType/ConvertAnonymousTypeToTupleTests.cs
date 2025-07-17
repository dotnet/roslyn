// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertAnonymousType;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAnonymousType;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
public sealed class ConvertAnonymousTypeToTupleTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpConvertAnonymousTypeToTupleCodeRefactoringProvider();

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Fact]
    public Task ConvertSingleAnonymousType()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                }
            }
            """);

    [Fact]
    public Task NotOnEmptyAnonymousType()
        => TestMissingInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { };
                }
            }
            """);

    [Fact]
    public Task NotOnSingleFieldAnonymousType()
        => TestMissingInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1 };
                }
            }
            """);

    [Fact]
    public Task ConvertSingleAnonymousTypeWithInferredName()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method(int b)
                {
                    var t1 = [||]new { a = 1, b };
                }
            }
            """, """
            class Test
            {
                void Method(int b)
                {
                    var t1 = (a: 1, b);
                }
            }
            """);

    [Fact]
    public Task ConvertMultipleInstancesInSameMethod()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                    var t2 = (a: 3, b: 4);
                }
            }
            """);

    [Fact]
    public Task ConvertMultipleInstancesAcrossMethods()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }

                void Method2()
                {
                    var t1 = new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                    var t2 = (a: 3, b: 4);
                }

                void Method2()
                {
                    var t1 = new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }
            }
            """);

    [Fact]
    public Task OnlyConvertMatchingTypesInSameMethod()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method(int b)
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    var t2 = new { a = 3, b };
                    var t3 = new { a = 4 };
                    var t4 = new { b = 5, a = 6 };
                }
            }
            """, """
            class Test
            {
                void Method(int b)
                {
                    var t1 = (a: 1, b: 2);
                    var t2 = (a: 3, b);
                    var t3 = new { a = 4 };
                    var t4 = new { b = 5, a = 6 };
                }
            }
            """);

    [Fact]
    public Task TestFixAllInSingleMethod()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method(int b)
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    var t2 = new { a = 3, b };
                    var t3 = new { a = 4 };
                    var t4 = new { b = 5, a = 6 };
                }
            }
            """, """
            class Test
            {
                void Method(int b)
                {
                    var t1 = (a: 1, b: 2);
                    var t2 = (a: 3, b);
                    var t3 = new { a = 4 };
                    var t4 = (b: 5, a: 6);
                }
            }
            """, index: 1);

    [Fact]
    public Task TestFixNotAcrossMethods()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }

                void Method2()
                {
                    var t1 = new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                    var t2 = (a: 3, b: 4);
                }

                void Method2()
                {
                    var t1 = new { a = 1, b = 2 };
                    var t2 = new { a = 3, b = 4 };
                }
            }
            """);

    [Fact]
    public Task TestTrivia()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = /*1*/ [||]new /*2*/ { /*3*/ a /*4*/ = /*5*/ 1 /*7*/ , /*8*/ b /*9*/ = /*10*/ 2 /*11*/ } /*12*/ ;
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = /*1*/ ( /*3*/ a /*4*/ : /*5*/ 1 /*7*/ , /*8*/ b /*9*/ : /*10*/ 2 /*11*/ ) /*12*/ ;
                }
            }
            """);

    [Fact]
    public Task TestFixAllNestedTypes()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = new { c = 1, d = 2 } };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: (c: 1, d: 2));
                }
            }
            """, index: 1);

    [Fact]
    public Task ConvertMultipleNestedInstancesInSameMethod()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = (object)new { a = 1, b = default(object) } };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: (object)(a: 1, b: default(object)));
                }
            }
            """);

    [Fact]
    public Task ConvertWithLambda1()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    Action a = () =>
                    {
                        var t2 = new { a = 3, b = 4 };
                    };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                    Action a = () =>
                    {
                        var t2 = (a: 3, b: 4);
                    };
                }
            }
            """);

    [Fact]
    public Task ConvertWithLambda2()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = new { a = 1, b = 2 };
                    Action a = () =>
                    {
                        var t2 = [||]new { a = 3, b = 4 };
                    };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                    Action a = () =>
                    {
                        var t2 = (a: 3, b: 4);
                    };
                }
            }
            """);

    [Fact]
    public Task ConvertWithLocalFunction1()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2 };
                    void func()
                    {
                        var t2 = new { a = 3, b = 4 };
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                    void func()
                    {
                        var t2 = (a: 3, b: 4);
                    }
                }
            }
            """);

    [Fact]
    public Task ConvertWithLocalFunction2()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = new { a = 1, b = 2 };
                    void func()
                    {
                        var t2 = [||]new { a = 3, b = 4 };
                    }
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                    void func()
                    {
                        var t2 = (a: 3, b: 4);
                    }
                }
            }
            """);

    [Fact]
    public Task TestIncompleteAnonymousType()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = , b = };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: , b: );
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34749")]
    public Task NotInExpressionTree()
        => TestMissingInRegularAndScriptAsync("""
            using System.Linq.Expressions;

            class C
            {
                static void Main(string[] args)
                {
                    Expression<Func<string, string, dynamic>> test =
                        (par1, par2) => [||]new { Parameter1 = par1, Parameter2 = par2 };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75950")]
    public Task RemoveTrailingComma()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = [||]new { a = 1, b = 2, };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = (a: 1, b: 2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50470")]
    public Task TestMultiLine1()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = var items = new[]
                    {
                        [||]new
                        {
                            x = 1,
                            y = 2,
                        },
                    };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = var items = new[]
                    {
                        (
                            x: 1,
                            y: 2
                        ),
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50470")]
    public Task TestMultiLine2()
        => TestInRegularAndScriptAsync("""
            class Test
            {
                void Method()
                {
                    var t1 = var items = new[]
                    {
                        [||]new
                        {
                            x = 1,
                            y = 2
                        },
                    };
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var t1 = var items = new[]
                    {
                        (
                            x: 1,
                            y: 2
                        ),
                    };
                }
            }
            """);
}
