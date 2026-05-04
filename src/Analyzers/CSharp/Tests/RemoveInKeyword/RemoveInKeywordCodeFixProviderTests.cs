// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveInKeyword;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveInKeyword;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveInKeyword)]
public sealed class RemoveInKeywordCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public RemoveInKeywordCodeFixProviderTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new RemoveInKeywordCodeFixProvider());

    [Fact]
    public Task TestRemoveInKeyword()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void M(int i) { }
                void N(int i)
                {
                    M(in [|i|]);
                }
            }
            """,
            """
            class Class
            {
                void M(int i) { }
                void N(int i)
                {
                    M(i);
                }
            }
            """);

    [Fact]
    public Task TestRemoveInKeywordMultipleArguments1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void M(int i, string s) { }
                void N(int i, string s)
                {
                    M(in [|i|], s);
                }
            }
            """,
            """
            class Class
            {
                void M(int i, string s) { }
                void N(int i, string s)
                {
                    M(i, s);
                }
            }
            """);

    [Fact]
    public Task TestRemoveInKeywordMultipleArguments2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void M(int i, int j) { }
                void N(int i, int j)
                {
                    M(in [|i|], in j);
                }
            }
            """,
            """
            class Class
            {
                void M(int i, int j) { }
                void N(int i, int j)
                {
                    M(i, in j);
                }
            }
            """);

    [Fact]
    public Task TestRemoveInKeywordMultipleArgumentsWithDifferentRefKinds()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void M(in int i, string s) { }
                void N(int i, string s)
                {
                    M(in i, in [|s|]);
                }
            }
            """,
            """
            class Class
            {
                void M(in int i, string s) { }
                void N(int i, string s)
                {
                    M(in i, s);
                }
            }
            """);

    [Fact]
    public Task TestDoNotRemoveInKeyword()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void M(in int i) { }
                void N(int i)
                {
                    M(in [|i|]);
                }
            }
            """);

    [Theory]
    [InlineData("in    [|i|]", "i")]
    [InlineData("  in  [|i|]", "  i")]
    [InlineData("/* start */in [|i|]", "/* start */i")]
    [InlineData("/* start */ in [|i|]", "/* start */ i")]
    [InlineData(
        "/* start */ in [|i|] /* end */",
        "/* start */ i /* end */")]
    [InlineData(
        "/* start */ in /* middle */ [|i|] /* end */",
        "/* start */ i /* end */")]
    [InlineData(
        "/* start */ in    /* middle */ [|i|] /* end */",
        "/* start */ i /* end */")]
    [InlineData(
        "/* start */in /* middle */ [|i|] /* end */",
        "/* start */i /* end */")]
    public Task TestRemoveInKeywordWithTrivia(string original, string expected)
        => TestInRegularAndScriptAsync(
            $$"""
            class App
            {
                void M(int i) { }
                void N(int i)
                {
                    M({{original}});
                }

            }
            """,
            $$"""
            class App
            {
                void M(int i) { }
                void N(int i)
                {
                    M({{expected}});
                }

            }
            """);

    [Fact]
    public Task TestRemoveInKeywordFixAllInDocument1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void M1(int i) { }
                void M2(int i, string s) { }

                void N1(int i)
                {
                    M1(in {|FixAllInDocument:i|});
                }

                void N2(int i, string s)
                {
                    M2(in i, in s);
                }

                void N3(int i, string s)
                {
                    M1(in i);
                    M2(in i, in s);
                }
            }
            """,
            """
            class Class
            {
                void M1(int i) { }
                void M2(int i, string s) { }

                void N1(int i)
                {
                    M1(i);
                }

                void N2(int i, string s)
                {
                    M2(i, s);
                }

                void N3(int i, string s)
                {
                    M1(i);
                    M2(i, s);
                }
            }
            """);

    [Fact]
    public Task TestRemoveInKeywordFixAllInDocument2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void M1(int i) { }
                void M2(in int i, string s) { }

                void N1(int i)
                {
                    M1(in {|FixAllInDocument:i|});
                }

                void N2(int i, string s)
                {
                    M2(in i, in s);
                }

                void N3(int i, string s)
                {
                    M1(in i);
                    M2(in i, in s);
                }
            }
            """,
            """
            class Class
            {
                void M1(int i) { }
                void M2(in int i, string s) { }

                void N1(int i)
                {
                    M1(i);
                }

                void N2(int i, string s)
                {
                    M2(in i, s);
                }

                void N3(int i, string s)
                {
                    M1(i);
                    M2(in i, s);
                }
            }
            """);
}
