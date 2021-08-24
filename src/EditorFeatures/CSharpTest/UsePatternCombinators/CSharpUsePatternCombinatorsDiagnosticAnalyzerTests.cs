// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UsePatternCombinators;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternCombinators
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUsePatternCombinatorsDiagnosticAnalyzer,
        CSharpUsePatternCombinatorsCodeFixProvider>;

    public class CSharpUsePatternCombinatorsDiagnosticAnalyzerTests
    {
        private static Task TestAllMissingOnExpressionAsync(string expression, LanguageVersion languageVersion = LanguageVersion.Latest, bool enabled = true)
            => TestMissingAsync(FromExpression(expression), languageVersion, enabled);

        private static async Task TestMissingAsync(string initialMarkup, LanguageVersion languageVersion = LanguageVersion.Latest, bool enabled = true)
        {
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
                LanguageVersion = languageVersion,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferPatternMatching, enabled, NotificationOption2.Silent }
                },
                DiagnosticVerifier = (x, y, z) => { },
            }.RunAsync();
        }

        private static async Task TestAllAsync(string initialMarkup, string expectedMarkup)
        {
            await VerifyCS.VerifyCodeFixAsync(initialMarkup, expectedMarkup);
        }

        private static Task TestAllOnExpressionAsync(string expression, string expected)
            => TestAllAsync(FromExpression(expression), FromExpression(expected));

        private static string FromExpression(string expression)
        {
            const string initialMarkup = @"
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
";
            return initialMarkup.Replace("EXPRESSION", expression);
        }

        [InlineData("i == 0")]
        [InlineData("i > 0")]
        [InlineData("o is C")]
        [InlineData("o is C c")]
        [InlineData("o != null")]
        [InlineData("!(o is null)")]
        [InlineData("o is int ii || o is long jj")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMissingOnExpression(string expression)
        {
            await TestAllMissingOnExpressionAsync(expression);
        }

        [InlineData("i == default || i > default(int)", "i is default(int) or > (default(int))")]
        [InlineData("!(o is C c)", "o is not C c")]
        [InlineData("o is int ii && o is long jj", "o is int ii and long jj")]
        [InlineData("!(o is C)", "o is not C")]
        [InlineData("!(o is C _)", "o is not C _")]
        [InlineData("i == (0x02 | 0x04) || i != 0", "i is (0x02 | 0x04) or not 0")]
        [InlineData("i == 1 || 2 == i", "i is 1 or 2")]
        [InlineData("i == (short)1 || (short)2 == i", "i is ((short)1) or ((short)2)")]
        [InlineData("nullable == 1 || 2 == nullable", "nullable is 1 or 2")]
        [InlineData("i != 1 || 2 != i", "i is not 1 or not 2")]
        [InlineData("i != 1 && 2 != i", "i is not 1 and not 2")]
        [InlineData("!(i != 1 && 2 != i)", "i is 1 or 2")]
        [InlineData("i < 1 && 2 <= i", "i is < 1 and >= 2")]
        [InlineData("i < 1 && 2 <= i && i is not 0", "i is < 1 and >= 2 and not 0")]
        [InlineData("(int.MaxValue - 1D) < i && i > 0", "i is > (int)(int.MaxValue - 1D) and > 0")]
        [InlineData("ch < ' ' || ch >= 0x100 || 'a' == ch", "ch is < ' ' or >= (char)0x100 or 'a'")]
        [InlineData("ch == 'a' || 'b' == ch", "ch is 'a' or 'b'")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestOnExpression(string expression, string expected)
        {
            await TestAllOnExpressionAsync(expression, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMissingIfDisabled()
        {
            await TestAllMissingOnExpressionAsync("o == 1 || o == 2", enabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMissingOnCSharp8()
        {
            await TestAllMissingOnExpressionAsync("o == 1 || o == 2", LanguageVersion.CSharp8);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMultilineTrivia_01()
        {
            await TestAllAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMultilineTrivia_02()
        {
            await TestAllAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestParenthesized()
        {
            await TestAllAsync(
@"class C
{
    bool M0(int v)
    {
        return {|FixAllInDocument:(v == 0 || v == 1 || v == 2)|};
    }
    bool M1(int v)
    {
        return (v == 0) || (v == 1) || (v == 2);
    }
}",
@"class C
{
    bool M0(int v)
    {
        return (v is 0 or 1 or 2);
    }
    bool M1(int v)
    {
        return v is 0 or 1 or 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestMissingInExpressionTree()
        {
            await TestMissingAsync(
@"using System.Linq;
class C
{
    void M0(IQueryable<int> q)
    {
        q.Where(item => item == 1 [||]|| item == 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        [WorkItem(52397, "https://github.com/dotnet/roslyn/issues/52397")]
        public async Task TestMissingInPropertyAccess_NullCheckOnLeftSide()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        [WorkItem(52397, "https://github.com/dotnet/roslyn/issues/52397")]
        public async Task TestMissingInPropertyAccess_NullCheckOnRightSide()
        {
            await TestMissingAsync(
@"using System;

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
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        [WorkItem(51691, "https://github.com/dotnet/roslyn/issues/51691")]
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

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        [WorkItem(51691, "https://github.com/dotnet/roslyn/issues/51691")]
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

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        [WorkItem(51693, "https://github.com/dotnet/roslyn/issues/51693")]
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

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        [WorkItem(52573, "https://github.com/dotnet/roslyn/issues/52573")]
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
    }
}
