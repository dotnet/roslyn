// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternCombinators
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
    public class CSharpUsePatternCombinatorsDiagnosticAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private static readonly ParseOptions CSharp9 = TestOptions.RegularPreview.WithLanguageVersion(LanguageVersion.CSharp9);

        private static readonly OptionsCollection s_disabled = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpCodeStyleOptions.PreferPatternMatching, new CodeStyleOption2<bool>(false, NotificationOption2.None) }
        };

        public CSharpUsePatternCombinatorsDiagnosticAnalyzerTests(ITestOutputHelper logger)
             : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUsePatternCombinatorsDiagnosticAnalyzer(), new CSharpUsePatternCombinatorsCodeFixProvider());

        private Task TestAllMissingOnExpressionAsync(string expression, ParseOptions? parseOptions = null, bool enabled = true)
            => TestMissingAsync(FromExpression(expression), parseOptions, enabled);

        private Task TestMissingAsync(string initialMarkup, ParseOptions? parseOptions = null, bool enabled = true)
            => TestMissingAsync(initialMarkup, new TestParameters(
                parseOptions: parseOptions ?? CSharp9, options: enabled ? null : s_disabled));

        private Task TestAllAsync(string initialMarkup, string expectedMarkup)
            => TestInRegularAndScriptAsync(initialMarkup, expectedMarkup,
                parseOptions: CSharp9, options: null);

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
        public async Task TestMissingOnExpression(string expression)
        {
            await TestAllMissingOnExpressionAsync(expression);
        }

        [InlineData("i == default || i > default(int)", "i is default(int) or > (default(int))")]
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
        public async Task TestOnExpression(string expression, string expected)
        {
            await TestAllOnExpressionAsync(expression, expected);
        }

        [InlineData("nullable == 1 || 2 == nullable", "nullable is 1 or 2")]
        [Theory]
        public async Task TestOnNullableExpression(string expression, string expected)
        {
            await TestAllOnExpressionAsync(expression, expected);
        }

        [Fact]
        public async Task TestMissingIfDisabled()
        {
            await TestAllMissingOnExpressionAsync("o == 1 || o == 2", enabled: false);
        }

        [Fact]
        public async Task TestMissingOnCSharp8()
        {
            await TestAllMissingOnExpressionAsync("o == 1 || o == 2", parseOptions: TestOptions.Regular8);
        }

        [Fact]
        public async Task TestMultilineTrivia_01()
        {
            await TestAllAsync(
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
        }

        [Fact]
        public async Task TestMultilineTrivia_02()
        {
            await TestAllAsync(
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
        }

        [Fact]
        public async Task TestParenthesized()
        {
            await TestAllAsync(
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
        }

        [Fact]
        public async Task TestMissingInExpressionTree()
        {
            await TestMissingAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52397")]
        public async Task TestMissingInPropertyAccess_NullCheckOnLeftSide()
        {
            await TestMissingAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52397")]
        public async Task TestMissingInPropertyAccess_NullCheckOnRightSide()
        {
            await TestMissingAsync(
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
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/51691")]
        [InlineData("&&")]
        [InlineData("||")]
        public async Task TestMissingInPropertyAccess_EnumCheckAndNullCheck(string logicalOperator)
        {
            await TestMissingAsync(
$@"using System.Diagnostics;

public class C
{{
    public void M()
    {{
            var p = default(Process);
            if (p.StartInfo.WindowStyle == ProcessWindowStyle.Hidden [|{logicalOperator}|] p.StartInfo != null)
            {{
            }}
    }}
}}");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/51691")]
        [InlineData("&&")]
        [InlineData("||")]
        public async Task TestMissingInPropertyAccess_EnumCheckAndNullCheckOnOtherType(string logicalOperator)
        {
            await TestMissingAsync(
$@"using System.Diagnostics;

public class C
{{
    public void M()
    {{
            var p = default(Process);
            if (p.StartInfo.WindowStyle == ProcessWindowStyle.Hidden [|{logicalOperator}|] this != null)
            {{
            }}
    }}
}}");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/51693")]
        [InlineData("&&")]
        [InlineData("||")]
        public async Task TestMissingInPropertyAccess_IsCheckAndNullCheck(string logicalOperator)
        {
            await TestMissingAsync(
$@"using System;

public class C
{{
    public void M()
    {{
            var o1 = new object();
            if (o1 is IAsyncResult ar [|{logicalOperator}|] ar.AsyncWaitHandle != null)
            {{
            }}
    }}
}}");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/52573")]
        [InlineData("&&")]
        [InlineData("||")]
        public async Task TestMissingIntegerAndStringIndex(string logicalOperator)
        {
            await TestMissingAsync(
$@"using System;

public class C
{{
    private static bool IsS(char[] ch, int count)
    {{
        return count == 1 [|{logicalOperator}|] ch[0] == 'S';
    }}
}}");
        }

        [Fact]
        public async Task TestOnSideEffects1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestOnSideEffects2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
        public async Task TestMissingInNonConvertibleTypePattern1()
        {
            await TestMissingAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
        public async Task TestMissingInNonConvertibleTypePattern2()
        {
            await TestMissingAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
        public async Task TestMissingInNonConvertibleTypePattern3()
        {
            await TestMissingAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
        public async Task TestInConvertibleTypePattern()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57199")]
        public async Task TestInConvertibleTypePattern2()
        {
            await TestInRegularAndScriptAsync(
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
        }
    }
}
