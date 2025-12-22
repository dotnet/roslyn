// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UsePatternCombinators;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternCombinators;

[Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
public sealed class CSharpUsePatternCombinatorsDiagnosticAnalyzerTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    private static readonly ParseOptions CSharp9 = TestOptions.RegularPreview.WithLanguageVersion(LanguageVersion.CSharp9);

    private static readonly OptionsCollection s_disabled = new(LanguageNames.CSharp)
    {
        { CSharpCodeStyleOptions.PreferPatternMatching, new CodeStyleOption2<bool>(false, NotificationOption2.None) }
    };

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUsePatternCombinatorsDiagnosticAnalyzer(), new CSharpUsePatternCombinatorsCodeFixProvider());

    private Task TestAllMissingOnExpressionAsync(string expression, ParseOptions? parseOptions = null, bool enabled = true)
        => TestMissingAsync(FromExpression(expression), parseOptions, enabled);

    private Task TestMissingAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup, ParseOptions? parseOptions = null, bool enabled = true)
        => TestMissingAsync(initialMarkup, new TestParameters(
            parseOptions: parseOptions ?? CSharp9, options: enabled ? null : s_disabled));

    private Task TestAllAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup)
        => TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(parseOptions: CSharp9, options: null));

    private Task TestAllOnExpressionAsync(string expression, string expected)
        => TestAllAsync(FromExpression(expression), FromExpression(expected));

    private static string FromExpression(string expression)
    {
        const string initialMarkup = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static bool field = {|FixAllInDocument:EXPRESSION|};
                static bool Method() => EXPRESSION;
                static bool Prop1 => EXPRESSION;
                static bool Prop2 { get; } = EXPRESSION;
                static void If() { if (EXPRESSION) ; }
                static void Argument1() => Test(EXPRESSION);
                static void Argument2() => Test(() => EXPRESSION);
                static void Argument3() => Test(_ => EXPRESSION);
                static void Test(bool b) {}
                static void Test(Func<bool> b) {}
                static void Test(Func<object, bool> b) {}
                static void For() { for (; EXPRESSION; ); }
                static void Local() { var local = EXPRESSION; }
                static void Conditional() { _ = EXPRESSION ? EXPRESSION : EXPRESSION; }
                static void Assignment() { _ = EXPRESSION; }
                static void Do() { do ; while (EXPRESSION); }
                static void While() { while (EXPRESSION) ; }
                static bool When() => o switch { _ when EXPRESSION => EXPRESSION };
                static bool Return() { return EXPRESSION; }
                static IEnumerable<bool> YieldReturn() { yield return EXPRESSION; }
                static Func<object, bool> SimpleLambda() => o => EXPRESSION;
                static Func<bool> ParenthesizedLambda() => () => EXPRESSION;
                static void LocalFunc() { bool LocalFunction() => EXPRESSION; }
                static int i;
                static int? nullable;
                static object o;
                static char ch;
            }
            """;
        return initialMarkup.Replace("EXPRESSION", expression);
    }

    [InlineData("i == 0")]
    [InlineData("i > 0")]
    [InlineData("o is C")]
    [InlineData("o is C c")]
    [InlineData("o != null")]
    [InlineData("!(o is null)")]
    [InlineData("o is int ii || o is long jj")]
    [Theory]
    public Task TestMissingOnExpression(string expression)
        => TestAllMissingOnExpressionAsync(expression);

    [InlineData("i == default || i > default(int)", "i is default(int) or > default(int)")]
    [InlineData("!(o is C c)", "o is not C c")]
    [InlineData("o is int ii && o is long jj", "o is int ii and long jj")]
    [InlineData("o is string || o is Exception", "o is string or Exception")]
    [InlineData("o is System.String || o is System.Exception", "o is System.String or System.Exception")]
    [InlineData("!(o is C)", "o is not C")]
    [InlineData("!(o is C _)", "o is not C _")]
    [InlineData("i == (0x02 | 0x04) || i != 0", "i is (0x02 | 0x04) or not 0")]
    [InlineData("i == 1 || 2 == i", "i is 1 or 2")]
    [InlineData("i == (short)1 || (short)2 == i", "i is ((short)1) or ((short)2)")]
    [InlineData("i != 1 || 2 != i", "i is not 1 or not 2")]
    [InlineData("i != 1 && 2 != i", "i is not 1 and not 2")]
    [InlineData("!(i != 1 && 2 != i)", "i is 1 or 2")]
    [InlineData("i < 1 && 2 <= i", "i is < 1 and >= 2")]
    [InlineData("i < 1 && 2 <= i && i is not 0", "i is < 1 and >= 2 and not 0")]
    [InlineData("(int.MaxValue - 1D) < i && i > 0", "i is > (int)(int.MaxValue - 1D) and > 0")]
    [InlineData("ch < ' ' || ch >= 0x100 || 'a' == ch", "ch is < ' ' or >= (char)0x100 or 'a'")]
    [InlineData("ch == 'a' || 'b' == ch", "ch is 'a' or 'b'")]
    [Theory]
    public Task TestOnExpression(string expression, string expected)
        => TestAllOnExpressionAsync(expression, expected);

    [InlineData("nullable == 1 || 2 == nullable", "nullable is 1 or 2")]
    [Theory]
    public Task TestOnNullableExpression(string expression, string expected)
        => TestAllOnExpressionAsync(expression, expected);

    [Fact]
    public Task TestMissingIfDisabled()
        => TestAllMissingOnExpressionAsync("o == 1 || o == 2", enabled: false);

    [Fact]
    public Task TestMissingOnCSharp8()
        => TestAllMissingOnExpressionAsync("o == 1 || o == 2", parseOptions: TestOptions.Regular8);

    [Fact]
    public Task TestMultilineTrivia_01()
        => TestAllAsync(
            """
            class C
            {
                bool M0(int variable)
                {
                    return {|FixAllInDocument:variable == 0 || /*1*/
                           variable == 1 || /*2*/
                           variable == 2|}; /*3*/
                }
                bool M1(int variable)
                {
                    return variable != 0 && /*1*/
                           variable != 1 && /*2*/
                           variable != 2; /*3*/
                }
            }
            """,
            """
            class C
            {
                bool M0(int variable)
                {
                    return variable is 0 or /*1*/
                           1 or /*2*/
                           2; /*3*/
                }
                bool M1(int variable)
                {
                    return variable is not 0 and /*1*/
                           not 1 and /*2*/
                           not 2; /*3*/
                }
            }
            """);

    [Fact]
    public Task TestMultilineTrivia_02()
        => TestAllAsync(
            """
            class C
            {
                bool M0(int variable)
                {
                    return {|FixAllInDocument:variable == 0 /*1*/
                        || variable == 1 /*2*/
                        || variable == 2|}; /*3*/
                }
                bool M1(int variable)
                {
                    return variable != 0 /*1*/
                        && variable != 1 /*2*/
                        && variable != 2; /*3*/
                }
            }
            """,
            """
            class C
            {
                bool M0(int variable)
                {
                    return variable is 0 /*1*/
                        or 1 /*2*/
                        or 2; /*3*/
                }
                bool M1(int variable)
                {
                    return variable is not 0 /*1*/
                        and not 1 /*2*/
                        and not 2; /*3*/
                }
            }
            """);

    [Fact]
    public Task TestParenthesized()
        => TestAllAsync(
            """
            class C
            {
                bool M0(int v)
                {
                    return {|FixAllInDocument:(v == 0 || v == 1 || v == 2)|};
                }
                bool M1(int v)
                {
                    return (v == 0) || (v == 1) || (v == 2);
                }
            }
            """,
            """
            class C
            {
                bool M0(int v)
                {
                    return (v is 0 or 1 or 2);
                }
                bool M1(int v)
                {
                    return v is 0 or 1 or 2;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66787")]
    public Task TestConvertedConstants()
        => TestAllAsync(
            """
            class C
            {
                bool M(long l)
                {
                    return {|FixAllInDocument:(l > int.MaxValue || l < int.MinValue)|};
                }
            }
            """,
            """
            class C
            {
                bool M(long l)
                {
                    return (l is > int.MaxValue or < int.MinValue);
                }
            }
            """);

    [Fact]
    public Task TestMissingInExpressionTree()
        => TestMissingAsync(
            """
            using System.Linq;
            class C
            {
                void M0(IQueryable<int> q)
                {
                    q.Where(item => item == 1 [||]|| item == 2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52397")]
    public Task TestMissingInPropertyAccess_NullCheckOnLeftSide()
        => TestMissingAsync(
            """
            using System;

            public class C
            {
                public int I { get; }

                public EventArgs Property { get; } 

                public void M()
                {
                    if (Property != null [|&&|] I == 1)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52397")]
    public Task TestMissingInPropertyAccess_NullCheckOnRightSide()
        => TestMissingAsync(
            """
            using System;

            public class C
            {
                public int I { get; }

                public EventArgs Property { get; } 

                public void M()
                {
                    if (I == 1 [|&&|] Property != null)
                    {
                    }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/51691")]
    [InlineData("&&")]
    [InlineData("||")]
    public Task TestMissingInPropertyAccess_EnumCheckAndNullCheck(string logicalOperator)
        => TestMissingAsync(
            $$"""
            using System.Diagnostics;

            public class C
            {
                public void M()
                {
                        var p = default(Process);
                        if (p.StartInfo.WindowStyle == ProcessWindowStyle.Hidden [|{{logicalOperator}}|] p.StartInfo != null)
                        {
                        }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/51691")]
    [InlineData("&&")]
    [InlineData("||")]
    public Task TestMissingInPropertyAccess_EnumCheckAndNullCheckOnOtherType(string logicalOperator)
        => TestMissingAsync(
            $$"""
            using System.Diagnostics;

            public class C
            {
                public void M()
                {
                        var p = default(Process);
                        if (p.StartInfo.WindowStyle == ProcessWindowStyle.Hidden [|{{logicalOperator}}|] this != null)
                        {
                        }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/51693")]
    [InlineData("&&")]
    [InlineData("||")]
    public Task TestMissingInPropertyAccess_IsCheckAndNullCheck(string logicalOperator)
        => TestMissingAsync(
            $$"""
            using System;

            public class C
            {
                public void M()
                {
                        var o1 = new object();
                        if (o1 is IAsyncResult ar [|{{logicalOperator}}|] ar.AsyncWaitHandle != null)
                        {
                        }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/52573")]
    [InlineData("&&")]
    [InlineData("||")]
    public Task TestMissingIntegerAndStringIndex(string logicalOperator)
        => TestMissingAsync(
            $$"""
            using System;

            public class C
            {
                private static bool IsS(char[] ch, int count)
                {
                    return count == 1 [|{{logicalOperator}}|] ch[0] == 'S';
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66787")]
    public Task TestMissingForImplicitUserDefinedCasts1()
        => TestMissingAsync(
            """
            using System;
            class C
            {
                void M0(Int128 i)
                {
                    if (i == int.MaxValue [||] i == int.MinValue)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66787")]
    public Task TestMissingForImplicitUserDefinedCasts2()
        => TestMissingAsync(
            """
            using System;
            class C
            {
                void M0(Int128 i)
                {
                    if (i > int.MaxValue [||] i < int.MinValue)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestOnSideEffects1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                char ReadChar() => default;

                void M(char c)
                {
                    if ({|FixAllInDocument:c == 'x' && c == 'y'|})
                    {
                    }

                    if (c == 'x' && c == 'y')
                    {
                    }

                    if (ReadChar() == 'x' && ReadChar() == 'y')
                    {
                    }
                }
            }
            """,

            """
            class C
            {
                char ReadChar() => default;

                void M(char c)
                {
                    if (c is 'x' and 'y')
                    {
                    }

                    if (c is 'x' and 'y')
                    {
                    }

                    if (ReadChar() == 'x' && ReadChar() == 'y')
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestOnSideEffects2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                char ReadChar() => default;

                void M(char c)
                {
                    if ({|FixAllInDocument:ReadChar() == 'x' && ReadChar() == 'y'|})
                    {
                    }

                    if (ReadChar() == 'x' && ReadChar() == 'y')
                    {
                    }

                    if (c == 'x' && c == 'y')
                    {
                    }
                }
            }
            """,

            """
            class C
            {
                char ReadChar() => default;

                void M(char c)
                {
                    if (ReadChar() is 'x' and 'y')
                    {
                    }

                    if (ReadChar() is 'x' and 'y')
                    {
                    }

                    if (c == 'x' && c == 'y')
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
    public Task TestMissingInNonConvertibleTypePattern1()
        => TestMissingAsync(
            """
            static class C
            {
                public struct S1 : I { }
                public struct S2 : I { }
                public interface I { }
            }

            class Test<T>
            {
                public readonly T C;
                bool P => [|C is C.S1 || C is C.S2|];
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
    public Task TestMissingInNonConvertibleTypePattern2()
        => TestMissingAsync(
            """
            class Goo
            {
                private class X { }
                private class Y { }

                private void M(object o)
                {
                    var X = 1;
                    var Y = 2;

                    if [|(o is X || o is Y)|]
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
    public Task TestMissingInNonConvertibleTypePattern3()
        => TestMissingAsync(
            """
            class Goo
            {
                private class X { }
                private class Y { }
                private void M(object o)
                {
                    var X = 1;
                    var Y = 2;
                    if [|(o is global::Goo.X || o is Y)|]
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
    public Task TestInConvertibleTypePattern()
        => TestInRegularAndScriptAsync(
            """
            static class C
            {
                public struct S1 : I { }
                public struct S2 : I { }
                public interface I { }
            }

            class Test<T>
            {
                bool P => [|C is C.S1 || C is C.S2|];
            }
            """,

            """
            static class C
            {
                public struct S1 : I { }
                public struct S2 : I { }
                public interface I { }
            }

            class Test<T>
            {
                bool P => C is C.S1 or C.S2;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
    public Task TestInConvertibleTypePattern2()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
                private class X { }
                private class Y { }

                private void M(object o)
                {
                    var X = 1;
                    var Y = 2;

                    var @int = 1;
                    var @long = 2;
                    if [|(o is int || o is long)|]
                    {
                    }
                }
            }
            """, """
            public class Goo
            {
                private class X { }
                private class Y { }

                private void M(object o)
                {
                    var X = 1;
                    var Y = 2;

                    var @int = 1;
                    var @long = 2;
                    if (o is int or long)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75122")]
    public Task TestNotWithMultipleCallsToInvocationWithRefArgument()
        => TestMissingAsync(
            """
            using System;

            static class DataUtils
            {
                internal static string ReadLine(byte[] bytes, ref int index)
                {
                    throw new NotImplementedException();
                }
            }

            class C
            {
                public void Main(byte[] bytes)
                {
                    int index = 0;

                    if ([|DataUtils.ReadLine(bytes, ref index) != "YAFC" || DataUtils.ReadLine(bytes, ref index) != "ProjectPage"|])
                    {
                        throw new InvalidDataException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76730")]
    public Task TestLogicalAndPatternNot()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void Main()
                {
                    var v = "";
                    if ([|!(v is not null)|])
                    {
                        Console.WriteLine("");
                    }
                }
            }
            """,
            """
            class C
            {
                static void Main()
                {
                    var v = "";
                    if (v is null)
                    {
                        Console.WriteLine("");
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80747")]
    public Task TestArithmeticParentheses()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void Main(string[] args)
                {
                    int a = 10;
                    _ = [|a > -1 && a < 100|];
                }
            }
            """,
            """
            class C
            {
                static void Main(string[] args)
                {
                    int a = 10;
                    _ = a is > -1 and < 100;
                }
            }
            """);
}
