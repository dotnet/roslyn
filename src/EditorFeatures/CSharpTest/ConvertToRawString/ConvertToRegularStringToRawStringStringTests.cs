// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ConvertToRawString;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRawString
{
    using VerifyCS = CSharpCodeRefactoringVerifier<
        ConvertRegularStringToRawStringCodeRefactoringProvider>;

    public class ConvertToRegularStringToRawStringStringTests
    {
        private static async Task VerifyRefactoringAsync(string testCode, string fixedCode, int index = 0, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext,
                CodeActionIndex = index,
                TestState =
                {
                    OutputKind = outputKind,
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotInDirective()
        {
            var code = @"
#line 1 [||]""goo.cs""";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnEmptyString()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]"""";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnEmptyVerbatimString()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]@"""";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnHighSurrogateChar()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]""\uD800"";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnLowSurrogateChar1()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]""\uDC00"";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestOnCombinedSurrogate()
        {
            await VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = [||]""\uD83D\uDC69"";
    }
}",
@"public class C
{
    void M()
    {
        var v = """"""👩"""""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnNullChar()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]""\u0000"";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnControlCharacter()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]""\u007F"";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestSimpleString()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]""a"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""a"""""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestVerbatimSimpleString()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""a"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""a"""""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestSimpleStringTopLevel()
        {
            await VerifyRefactoringAsync(@"
var v = [||]""a"";
", @"
var v = """"""a"""""";
", outputKind: OutputKind.ConsoleApplication);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestStringWithQuoteInMiddle()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]""goo\""bar"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""goo""bar"""""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestVerbatimStringWithQuoteInMiddle()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""goo""""bar"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""goo""bar"""""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestStringWithQuoteAtStart()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]""\""goobar"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            ""goobar
            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestVerbatimStringWithQuoteAtStart()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""""""goobar"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            ""goobar
            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestStringWithQuoteAtEnd()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]""goobar\"""";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            goobar""
            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestVerbatimStringWithQuoteAtEnd()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""goobar"""""";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            goobar""
            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestStringWithNewLine()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]""goo\r\nbar"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            goo
            bar
            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestVerbatimStringWithNewLine()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""goo
bar"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            goo
            bar
            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestStringWithNewLineAtStartAndEnd()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]""\r\ngoobar\r\n"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""

            goobar

            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestVerbatimStringWithNewLineAtStartAndEnd()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""
goobar
"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""

            goobar

            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNoIndentVerbatimStringWithNewLineAtStartAndEnd()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""
goobar
"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            goobar
            """""";
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestIndentedString()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]""goo\r\nbar"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            goo
            bar
            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestWithoutLeadingWhitespace1()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""
from x in y
where x > 0
select x"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            from x in y
            where x > 0
            select x
            """""";
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestIndentedStringTopLevel()
        {
            await VerifyRefactoringAsync(@"
var v = [||]""goo\r\nbar"";
", @"
var v = """"""
    goo
    bar
    """""";
", outputKind: OutputKind.ConsoleApplication);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestWithoutLeadingWhitespaceTopLevel()
        {
            await VerifyRefactoringAsync(@"
var v = [||]@""
from x in y
where x > 0
select x"";
", @"
var v = """"""
    from x in y
    where x > 0
    select x
    """""";
", index: 1, outputKind: OutputKind.ConsoleApplication);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestVerbatimIndentedString()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""goo
bar"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            goo
            bar
            """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestIndentedStringOnOwnLine()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v =
                [||]""goo\r\nbar"";
    }
}", @"public class C
{
    void M()
    {
        var v =
                """"""
                goo
                bar
                """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestVerbatimIndentedStringOnOwnLine()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v =
                [||]@""goo
bar"";
    }
}", @"public class C
{
    void M()
    {
        var v =
                """"""
                goo
                bar
                """""";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestWithoutLeadingWhitespace2()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""
            from x in y
            where x > 0
            select x"";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            from x in y
            where x > 0
            select x
            """""";
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestWithoutLeadingWhitespace3()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""
            from x in y
            where x > 0
            select x
            "";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            from x in y
            where x > 0
            select x
            """""";
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestWithoutLeadingWhitespace4()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""
            from x in y
                where x > 0
                select x
            "";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            from x in y
                where x > 0
                select x
            """""";
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestWithoutLeadingWhitespace5()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""
                from x in y
            where x > 0
            select x
            "";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
                from x in y
            where x > 0
            select x
            """""";
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestWithoutLeadingWhitespace6()
        {
            await VerifyRefactoringAsync(@"public class C
{
    void M()
    {
        var v = [||]@""
            from x in y

            where x > 0

            select x
            "";
    }
}", @"public class C
{
    void M()
    {
        var v = """"""
            from x in y

            where x > 0

            select x
            """""";
    }
}", index: 1);
        }
    }
}
