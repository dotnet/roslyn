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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertConditional
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
    public class InvertConditionalTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpInvertConditionalCodeRefactoringProvider();

        [Fact]
        public async Task InvertConditional1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task InvertConditional2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestTrivia()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestTrivia2()
        {
            // We currently do not move trivia along with the true/false parts.  We could consider
            // trying to intelligently do that in the future.  It would require moving the comments,
            // but preserving the whitespace/newlines.
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestStartOfConditional()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestAfterCondition()
        {
            await TestInRegularAndScriptAsync(
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
    }
}
