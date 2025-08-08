// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.InvertLogical;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertLogical;

[Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
public sealed partial class InvertLogicalTests : AbstractCSharpCodeActionTest_NoEditor
{
    private static readonly ParseOptions CSharp6 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6);
    private static readonly ParseOptions CSharp8 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);
    private static readonly ParseOptions CSharp9 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpInvertLogicalCodeRefactoringProvider();

    [Fact]
    public Task InvertLogical1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = a > 10 [||]|| b < 20;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !(a <= 10 && b >= 20);
                }
            }
            """);

    [Fact]
    public Task InvertLogical2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !(a <= 10 [||]&& b >= 20);
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = a > 10 || b < 20;
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
                    var c = !(a <= 10 [||]&&
                              b >= 20);
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = a > 10 ||
                              b < 20;
                }
            }
            """);

    [Fact]
    public Task TestTrivia2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b, int c)
                {
                    var c = !(a <= 10 [||]&&
                              b >= 20 &&
                              c == 30);
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b, int c)
                {
                    var c = a > 10 ||
                              b < 20 ||
                              c != 30;
                }
            }
            """);

    [Fact]
    public Task InvertMultiConditional1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = a > 10 [||]|| b < 20 || c == 30;
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = !(a <= 10 && b >= 20 && c != 30);
                }
            }
            """);

    [Fact]
    public Task InvertMultiConditional2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = a > 10 || b < 20 [||]|| c == 30;
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = !(a <= 10 && b >= 20 && c != 30);
                }
            }
            """);

    [Fact]
    public Task InvertMultiConditional3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = !(a <= 10 [||]&& b >= 20 && c != 30);
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = a > 10 || b < 20 || c == 30;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task InverSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = !([|a <= 10 && b >= 20 && c != 30|]);
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = a > 10 || b < 20 || c == 30;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task MissingInverSelection1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = !([|a <= 10 && b >= 20|] && c != 30);
                }
            }
            """/*
@"class C
{
void M(int a, int b, int c)
{
    var x = !(!(a > 10 || b < 20) && c != 30);
}
}"*/);

    [Fact]
    public Task InvertMultiConditional4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = !(a <= 10 && b >= 20 [||]&& c != 30);
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = a > 10 || b < 20 || c == 30;
                }
            }
            """);

    [Fact]
    public Task TestMissingOnShortCircuitAnd()
        => TestMissingAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = a > 10 [||]& b < 20;
                }
            }
            """);

    [Fact]
    public Task TestMissingOnShortCircuitOr()
        => TestMissingAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = a > 10 [||]| b < 20;
                }
            }
            """);

    [Fact]
    public Task TestSelectedOperator()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = a > 10 [||||] b < 20;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, int b)
                {
                    var c = !(a <= 10 && b >= 20);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task MissingSelectedSubtree()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    var x = !(a <= 10 && [|b >= 20 && c != 30|]);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsTypePattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is string));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsTypePattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is not string);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsNotTypePattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is not string;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is string);
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsNotTypePattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is not string;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is string);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsNullPattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is null;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is null));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsNullPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is null;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is not null);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsNotNullPattern1_CSharp6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is not null;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is null);
                }
            }
            """, new(parseOptions: CSharp6));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsNotNullPattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is not null;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is null);
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsNotNullPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is not null;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is null);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsTruePattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is true;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is true));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertBooleanIsTruePattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& x is true;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || x is false);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64292")]
    public Task InvertNonBooleanIsTruePattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is true;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is not true);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsFalsePattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is false;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is false));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertBooleanIsFalsePattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& x is false;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || x is true);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64292")]
    public Task InvertNonBooleanIsFalsePattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is false;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is not false);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64558")]
    public Task InvertNumericIsGreaterThanPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& a is > 20;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || a is <= 20);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64558")]
    public Task InvertNullableNumericIsGreaterThanPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int? a, object b)
                {
                    var c = x [||]&& a is > 20;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int? a, object b)
                {
                    var c = !(!x || a is not > 20);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64558")]
    public Task InvertNonNumericIsGreaterThanPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is > 20;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is not > 20);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64558")]
    public Task InvertInvalidEqualsPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& a is == 20;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || a is not == 20);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsAndPattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string and object;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is string and object));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsAndPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string and object;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is not string or not object);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsOrPattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string or object;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is string or object));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsOrPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string or object;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is not string and not object);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsTypeWithDesignationPattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string s;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is string s));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsTypeWithDesignationPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string s;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || b is not string s);
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsVarPattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is var s;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is var s));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsVarPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is var s;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is var s));
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsAndWithDesignationPattern1_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string s and object;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is string s and object));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsAndWithDesignationPattern1_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string s and object;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is string s and object));
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsAndWithDesignationPattern2_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string and object s;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is string and object s));
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task InvertIsAndWithDesignationPattern2_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = a > 10 [||]&& b is string and object s;
                }
            }
            """,
            """
            class C
            {
                void M(bool x, int a, object b)
                {
                    var c = !(a <= 10 || !(b is string and object s));
                }
            }
            """, new(parseOptions: CSharp9));
}
