// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.BraceHighlighting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BraceHighlighting;

[Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
public sealed class BraceHighlightingTests : AbstractBraceHighlightingTests
{
    protected override EditorTestWorkspace CreateWorkspace(string markup, ParseOptions options)
        => EditorTestWorkspace.CreateCSharp(markup, parseOptions: options);

    [WpfTheory]
    [InlineData("""
        public class C$$ {
        }
        """)]
    [InlineData("""
        public class C $$[|{|]
        [|}|]
        """)]
    [InlineData("""
        public class C {$$
        }
        """)]
    [InlineData("""
        public class C {
        $$}
        """)]
    [InlineData("""
        public class C [|{|]
        [|}|]$$
        """)]
    public Task TestCurlies(string testCase)
        => TestBraceHighlightingAsync(testCase);

    [WpfTheory]
    [InlineData("""
        public class C $$[|{|]
          public void Goo(){}
        [|}|]
        """)]
    [InlineData("""
        public class C {$$
          public void Goo(){}
        }
        """)]
    [InlineData("""
        public class C {
          public void Goo$$[|(|][|)|]{}
        }
        """)]
    [InlineData("""
        public class C {
          public void Goo($$){}
        }
        """)]
    [InlineData("""
        public class C {
          public void Goo[|(|][|)|]$$[|{|][|}|]
        }
        """)]
    [InlineData("""
        public class C {
          public void Goo(){$$}
        }
        """)]
    [InlineData("""
        public class C {
          public void Goo()[|{|][|}|]$$
        }
        """)]
    public Task TestTouchingItems(string testCase)
        => TestBraceHighlightingAsync(testCase);

    [WpfTheory]
    [InlineData("/// $$<summary>Goo</summary>")]
    [InlineData("/// <$$summary>Goo</summary>")]
    [InlineData("/// <summary$$>Goo</summary>")]
    [InlineData("/// <summary>$$Goo</summary>")]
    [InlineData("/// <summary>Goo$$</summary>")]
    [InlineData("/// <summary>Goo<$$/summary>")]
    [InlineData("/// <summary>Goo</$$summary>")]
    [InlineData("/// <summary>Goo</summary$$>")]
    [InlineData("/// <summary>Goo</summary>$$")]

    [InlineData("public class C$$[|<|]T[|>|] { }")]
    [InlineData("public class C<$$T> { }")]
    [InlineData("public class C<T$$> { }")]
    [InlineData("public class C[|<|]T[|>$$|] { }")]

    [InlineData("unsafe class C { delegate*$$[|<|] int, int[|>|] functionPointer; }")]
    [InlineData("unsafe class C { delegate*[|<|]int, int[|>$$|] functionPointer; }")]
    [InlineData("unsafe class C { delegate*<int, delegate*[|<|]int, int[|>|]$$> functionPointer; }")]
    public Task TestAngles(string testCase)
        => TestBraceHighlightingAsync(testCase);

    [WpfFact]
    public async Task TestNoHighlightingOnOperators()
    {
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void Goo()
                {
                    bool a = b $$< c;
                    bool d = e > f;
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void Goo()
                {
                    bool a = b <$$ c;
                    bool d = e > f;
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void Goo()
                {
                    bool a = b < c;
                    bool d = e $$> f;
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void Goo()
                {
                    bool a = b < c;
                    bool d = e >$$ f;
                }
            }
            """);
    }

    [WpfFact]
    public async Task TestSwitch()
    {
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void M(int variable)
                {
                    switch $$[|(|]variable[|)|]
                    {
                        case 0:
                            break;
                    }
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void M(int variable)
                {
                    switch ($$variable)
                    {
                        case 0:
                            break;
                    }
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void M(int variable)
                {
                    switch (variable$$)
                    {
                        case 0:
                            break;
                    }
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void M(int variable)
                {
                    switch [|(|]variable[|)$$|]
                    {
                        case 0:
                            break;
                    }
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void M(int variable)
                {
                    switch (variable)
                    $$[|{|]
                        case 0:
                            break;
                    [|}|]
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void M(int variable)
                {
                    switch (variable)
                    {$$
                        case 0:
                            break;
                    }
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void M(int variable)
                {
                    switch (variable)
                    {
                        case 0:
                            break;
                    $$}
                }
            }
            """);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                void M(int variable)
                {
                    switch (variable)
                    [|{|]
                        case 0:
                            break;
                    [|}$$|]
                }
            }
            """);
    }

    [WpfFact]
    public async Task TestEOF()
    {
        await TestBraceHighlightingAsync("""
            public class C [|{|]
            [|}|]$$
            """);
        await TestBraceHighlightingAsync("""
            public class C [|{|]
             void Goo(){}[|}|]$$
            """);
    }

    [WpfFact]
    public async Task TestTuples()
    {
        await TestBraceHighlightingAsync(
            """
            class C
            {
                [|(|]int, int[|)$$|] x = (1, 2);
            }
            """, TestOptions.Regular);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                (int, int) x = [|(|]1, 2[|)$$|];
            }
            """, TestOptions.Regular);
    }

    [WpfFact]
    public async Task TestNestedTuples()
    {
        await TestBraceHighlightingAsync(
            """
            class C
            {
                ([|(|]int, int[|)$$|], string) x = ((1, 2), "hello";
            }
            """, TestOptions.Regular);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                ((int, int), string) x = ([|(|]1, 2[|)$$|], "hello";
            }
            """, TestOptions.Regular);
    }

    [WpfFact]
    public async Task TestTuplesWithGenerics()
    {
        await TestBraceHighlightingAsync(
            """
            class C
            {
                [|(|]Dictionary<int, string>, List<int>[|)$$|] x = (null, null);
            }
            """, TestOptions.Regular);
        await TestBraceHighlightingAsync(
            """
            class C
            {
                var x = [|(|]new Dictionary<int, string>(), new List<int>()[|)$$|];
            }
            """, TestOptions.Regular);
    }

    [WpfFact]
    public Task TestRegexGroupBracket1()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"$$[|(|]a[|)|]");
                }
            }
            """);

    [WpfFact]
    public Task TestRegexGroupBracket2()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"[|(|]a[|)|]$$");
                }
            }
            """);

    [WpfFact]
    public Task TestRegexUnclosedGroupBracket1()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"$$(a");
                }
            }
            """);

    [WpfFact]
    public Task TestRegexCommentBracket1()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"$$[|(|]?#a[|)|]");
                }
            }
            """);

    [WpfFact]
    public Task TestRegexCommentBracket2()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"[|(|]?#a[|)|]$$");
                }
            }
            """);

    [WpfFact]
    public Task TestRegexUnclosedCommentBracket()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"$$(?#a");
                }
            }
            """);

    [WpfFact]
    public Task TestRegexCharacterClassBracket1()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"$$[|<|]a[|>|]");
                }
            }
            """, swapAnglesWithBrackets: true);

    [WpfFact]
    public Task TestRegexCharacterClassBracket2()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"[|<|]a[|>|]$$");
                }
            }
            """, swapAnglesWithBrackets: true);

    [WpfFact]
    public Task TestRegexUnclosedCharacterClassBracket1()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"$$<a");
                }
            }
            """, swapAnglesWithBrackets: true);

    [WpfFact]
    public Task TestRegexNegativeCharacterClassBracket1()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"$$[|<|]^a[|>|]");
                }
            }
            """, swapAnglesWithBrackets: true);

    [WpfFact]
    public Task TestRegexNegativeCharacterClassBracket2()
        => TestBraceHighlightingAsync("""
            using System.Text.RegularExpressions;

            class C
            {
                void Goo()
                {
                    var r = new Regex(@"[|<|]^a[|>|]$$");
                }
            }
            """, swapAnglesWithBrackets: true);

    [WpfFact]
    public Task TestJsonBracket1()
        => TestBraceHighlightingAsync("""
            class C
            {
                void Goo()
                {
                    var r = /*lang=json*/ @"new Json[|$$(|]1, 2, 3[|)|]";
                }
            }
            """);

    [WpfFact]
    public Task TestJsonBracket2()
        => TestBraceHighlightingAsync("""
            class C
            {
                void Goo()
                {
                    var r = /*lang=json*/ @"new Json[|(|]1, 2, 3[|)|]$$";
                }
            }
            """);

    [WpfFact]
    public Task TestJsonBracket_RawStrings()
        => TestBraceHighlightingAsync(""""
            class C
            {
                void Goo()
                {
                    var r = /*lang=json*/ """new Json[|$$(|]1, 2, 3[|)|]""";
                }
            }
            """");

    [WpfFact]
    public Task TestUnmatchedJsonBracket1()
        => TestBraceHighlightingAsync("""
            class C
            {
                void Goo()
                {
                    var r = /*lang=json*/ @"new Json$$(1, 2, 3";
                }
            }
            """);

    [WpfFact]
    public Task TestJsonBracket_NoComment_NotLikelyJson()
        => TestBraceHighlightingAsync("""
            class C
            {
                void Goo()
                {
                    var r = @"$$[ 1, 2, 3 ]";
                }
            }
            """);

    [WpfFact]
    public Task TestJsonBracket_NoComment_LikelyJson()
        => TestBraceHighlightingAsync("""
            class C
            {
                void Goo()
                {
                    var r = @"[ { prop: 0 }, new Json[|$$(|]1, 2, 3[|)|], 3 ]";
                }
            }
            """);

    [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/32791")]
    [InlineData(@"$$ /* goo */ public class C { }")]
    [InlineData(@" $$[|/*|] goo [|*/|] public class C { }")]
    [InlineData(@" [|/$$*|] goo [|*/|] public class C { }")]
    [InlineData(@" /*$$ goo */ public class C { }")]
    [InlineData(@" /* $$goo */ public class C { }")]
    [InlineData(@" /* goo$$ */ public class C { }")]
    [InlineData(@" /* goo $$*/ public class C { }")]
    [InlineData(@" [|/*|] goo [|*$$/|] public class C { }")]
    [InlineData(@" [|/*|] goo [|*/|]$$ public class C { }")]
    [InlineData(@" /* goo */ $$public class C { }")]
    public Task TestBlockComments(string input)
        => TestBraceHighlightingAsync(input);

    [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/32791")]
    [InlineData(@"$$ /** goo */ public class C { }")]
    [InlineData(@" $$[|/**|] goo [|*/|] public class C { }")]
    [InlineData(@" [|/$$**|] goo [|*/|] public class C { }")]
    [InlineData(@" [|/*$$*|] goo [|*/|] public class C { }")]
    [InlineData(@" /**$$ goo */ public class C { }")]
    [InlineData(@" /** $$goo */ public class C { }")]
    [InlineData(@" /** goo$$ */ public class C { }")]
    [InlineData(@" /** goo $$*/ public class C { }")]
    [InlineData(@" [|/**|] goo [|*$$/|] public class C { }")]
    [InlineData(@" [|/**|] goo [|*/|]$$ public class C { }")]
    [InlineData(@" /** goo */ $$public class C { }")]
    public Task TestDocCommentBlockComments(string input)
        => TestBraceHighlightingAsync(input);
}
