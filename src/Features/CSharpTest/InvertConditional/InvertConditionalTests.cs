// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.InvertConditional;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertConditional;

[Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
public sealed class InvertConditionalTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpInvertConditionalCodeRefactoringProvider();

    [Fact]
    public Task InvertConditional1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = x [||]? a : b;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !x ? b : a;
                }
            }
            """);

    [Fact]
    public Task InvertConditional2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !x [||]? a : b;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = x ? b : a;
                }
            }
            """);

    [Fact]
    public Task TestTrivia()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = [||]x
                        ? a
                        : b;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !x
                        ? b
                        : a;
                }
            }
            """);

    [Fact]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = [||]x ?
                        a :
                        b;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !x ?
                        b :
                        a;
                }
            }
            """);

    [Fact]
    public Task TestTrivia2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = [||]x
                        ? a /*trivia1*/
                        : b /*trivia2*/;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !x
                        ? b /*trivia1*/
                        : a /*trivia2*/;
                }
            }
            """);

    [Fact]
    public Task TestStartOfConditional()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = [||]x ? a : b;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !x ? b : a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestAfterCondition()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = x ? a [||]: b;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !x ? b : a;
                }
            }
            """);
}
