// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class SemanticClassifierTests : AbstractCSharpClassifierTests
    {
        [Theory, CombinatorialData]
        public async Task TestStringEscape1(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = ""goo\r\nbar"";",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"\n"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape1_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = ""goo\r\nbar""u8;",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"\n"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape2(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = @""goo\r\nbar"";",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape2_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = @""goo\r\nbar""u8;",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape3(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo{{1}}bar"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape3_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo{{1}}bar""u8;",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape4(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""goo{{1}}bar"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape4_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""goo{{1}}bar""u8;",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape5(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo\r{{1}}\nbar"";",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"{{"),
                Escape(@"}}"),
                Escape(@"\n"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape5_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo\r{{1}}\nbar""u8;",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"{{"),
                Escape(@"}}"),
                Escape(@"\n"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape6(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""goo\r{{1}}\nbar"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape6_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""goo\r{{1}}\nbar""u8;",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape7(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo\r{1}\nbar"";",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"\n"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape7_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $""goo\r{1}\nbar""u8;",
                testHost,
                Keyword("var"),
                Escape(@"\r"),
                Escape(@"\n"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""{{goo{1}bar}}"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape8_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""{{goo{1}bar}}""u8;",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape9(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""{{{12:X}}}"";",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestStringEscape9_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = $@""{{{12:X}}}""u8;",
                testHost,
                Keyword("var"),
                Escape(@"{{"),
                Escape(@"}}"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral1(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = """"""goo\r\nbar"""""";",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral1_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = """"""goo\r\nbar""""""u8;",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral2(TestHost testHost)
        {
            await TestInMethodAsync(""""
                var goo = """
                    goo\r\nbar
                    """;
                """",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral2_utf8(TestHost testHost)
        {
            await TestInMethodAsync(""""
                var goo = """
                    goo\r\nbar
                    """u8;
                """",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral3(TestHost testHost)
        {
            await TestInMethodAsync(""""
                var goo = $"""
                    goo\r\nbar
                    """;
                """",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral3_utf8(TestHost testHost)
        {
            await TestInMethodAsync(""""
                var goo = $"""
                    goo\r\nbar
                    """u8;
                """",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral4(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = """"""\"""""";",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral4_utf8(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = """"""\""""""u8;",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral5(TestHost testHost)
        {
            await TestInMethodAsync(""""
                var goo = """
                    \
                    """;
                """",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral5_utf8(TestHost testHost)
        {
            await TestInMethodAsync(""""
                var goo = """
                    \
                    """u8;
                """",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral6(TestHost testHost)
        {
            await TestInMethodAsync(""""
                var goo = $"""
                    \
                    """;
                """",
                testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestNotStringEscapeInRawLiteral6_utf8(TestHost testHost)
        {
            await TestInMethodAsync(""""
                var goo = $"""
                    \
                    """u8;
                """",
                testHost,
                Keyword("var"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31200")]
        [CombinatorialData]
        public async Task TestCharEscape1(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '\n';",
                testHost,
                Keyword("var"),
                Escape(@"\n"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31200")]
        [CombinatorialData]
        public async Task TestCharEscape2(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '\\';",
                testHost,
                Keyword("var"),
                Escape(@"\\"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31200")]
        [CombinatorialData]
        public async Task TestCharEscape3(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '\'';",
                testHost,
                Keyword("var"),
                Escape(@"\'"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31200")]
        [CombinatorialData]
        public async Task TestCharEscape5(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '""';",
                testHost,
                Keyword("var"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31200")]
        [CombinatorialData]
        public async Task TestCharEscape4(TestHost testHost)
        {
            await TestInMethodAsync(@"var goo = '\u000a';",
                testHost,
                Keyword("var"),
                Escape(@"\u000a"));
        }

        [Theory, CombinatorialData]
        public async Task TestEscapeFourBytesCharacter(TestHost testHost)
        {
            await TestAsync("""
                class C
                {
                        void M()
                        {
                            var x = "𠀀𠀁𠣶𤆐𥽠𪛕";
                        }
                }
                """, testHost,
                Keyword("var"));
        }

        [Theory, CombinatorialData]
        public async Task TestEscapeCharacter(TestHost testHost)
        {
            await TestAsync("""""
                class C
                {
                    string x1 = "\xabcd";
                    string x2 = "\uabcd";
                    string x3 = "\U00009F99";
                    string x4 = "\'";
                    string x5 = "\"";
                    string x6 = "\\";
                    string x7 = "\0";
                    string x8 = "\a";
                    string x9 = "\b";
                    string x10 = "\f";
                    string x11 = "\n";
                    string x12 = "\r";
                    string x13 = "\t";
                    string x14 = "\v";
                    string x15 = @"""";
                }
                """"", testHost,
                Escape("\\xabcd"),
                Escape("\\uabcd"),
                Escape("\\U00009F99"),
                Escape("\\\'"),
                Escape("""
                    \"
                    """),
                Escape("\\\\"),
                Escape("\\0"),
                Escape("\\a"),
                Escape("\\b"),
                Escape("\\f"),
                Escape("\\n"),
                Escape("\\r"),
                Escape("\\t"),
                Escape("\\v"),
                Escape("""
                    ""
                    """));
        }
    }
}
