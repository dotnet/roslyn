// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.SpellCheck;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SpellCheck;

[UseExportProvider]
public class SpellCheckSpanTests : AbstractSpellCheckSpanTests
{
    protected override EditorTestWorkspace CreateWorkspace(string content)
        => EditorTestWorkspace.CreateCSharp(content);

    [Fact]
    public async Task TestSingleLineComment1()
    {
        await TestAsync("{|Comment:// Goo|}");
    }

    [Fact]
    public async Task TestSingleLineComment2()
    {
        await TestAsync("""
            {|Comment:// Goo|}
            """);
    }

    [Fact]
    public async Task TestMultiLineComment1()
    {
        await TestAsync("{|Comment:/* Goo */|}");
    }

    [Fact]
    public async Task TestMultiLineComment2()
    {
        await TestAsync("""
            {|Comment:/*
               Goo
             */|}
            """);
    }

    [Fact]
    public async Task TestMultiLineComment3()
    {
        await TestAsync("""
            {|Comment:/*
               Goo
             |}
            """);
    }

    [Fact]
    public async Task TestMultiLineComment4()
    {
        await TestAsync("""
            {|Comment:/**/|}
            """);
    }

    [Fact]
    public async Task TestMultiLineComment5()
    {
        await TestAsync("""
            {|Comment:/*/|}
            """);
    }

    [Fact]
    public async Task TestDocComment1()
    {
        await TestAsync("""
            ///{|Comment:goo bar baz|}
            class {|Identifier:C|}
            {
            }
            """);
    }

    [Fact]
    public async Task TestDocComment2()
    {
        await TestAsync("""
            ///{|Comment:goo bar baz|}
            ///{|Comment:goo bar baz|}
            class {|Identifier:C|}
            {
            }
            """);
    }

    [Fact]
    public async Task TestDocComment3()
    {
        await TestAsync("""
            ///{|Comment: |}<summary>{|Comment: goo bar baz |}</summary>
            class {|Identifier:C|}
            {
            }
            """);
    }

    [Fact]
    public async Task TestString1()
    {
        await TestAsync(@"{|String:"" goo ""|}");
    }

    [Fact]
    public async Task TestString2()
    {
        await TestAsync("""
            " goo
            """);
    }

    [Fact]
    public async Task TestString3()
    {
        await TestAsync("""
            {|String:" goo "|}
            """);
    }

    [Fact]
    public async Task TestString4()
    {
        await TestAsync("""
            " goo
            """);
    }

    [Fact]
    public async Task TestString5()
    {
        await TestAsync("""
            {|String:@" goo "|}
            """);
    }

    [Fact]
    public async Task TestString6()
    {
        await TestAsync("""
            @" goo
            """);
    }

    [Fact]
    public async Task TestString7()
    {
        await TestAsync(@"{|String:"""""" goo """"""|}");
    }

    [Fact]
    public async Task TestString8()
    {
        await TestAsync(""""
            """ goo ""
            """");
    }

    [Fact]
    public async Task TestString9()
    {
        await TestAsync(""""
            """ goo "
            """");
    }

    [Fact]
    public async Task TestString10()
    {
        await TestAsync(""""
            """ goo
            """");
    }

    [Fact]
    public async Task TestString11()
    {
        await TestAsync(""""
            {|String:"""
                goo 
                """|}
            """");
    }

    [Fact]
    public async Task TestString12()
    {
        await TestAsync(""""
            """
                goo
                ""
            """");
    }

    [Fact]
    public async Task TestString13()
    {
        await TestAsync(""""
            """
                goo
                "
            """");
    }

    [Fact]
    public async Task TestString14()
    {
        await TestAsync(""""
            """
                goo
            """");
    }

    [Fact]
    public async Task TestString15()
    {
        await TestAsync("""
            $"{|String: goo |}"
            """);
    }

    [Fact]
    public async Task TestString16()
    {
        await TestAsync("""
            $"{|String: goo |}{0}{|String: bar |}"
            """);
    }

    [Fact]
    public async Task TestString17()
    {
        await TestAsync(""""
            $"""{|String: goo |}{0}{|String: bar |}"""
            """");
    }

    [Fact]
    public async Task TestString18()
    {
        await TestAsync(""""
            $"""{|String: goo |}{0:abcd}{|String: bar |}"""
            """");
    }

    [Fact]
    public async Task TestEscapedString1()
    {
        await TestAsync(""" " {|String:goo|}\r\n{|String:bar |}" """);
    }

    [Fact]
    public async Task TestEscapedString2()
    {
        await TestAsync(""" " {|String:goo|}\r\t{|String:bar |}" """);
    }

    [Fact]
    public async Task TestEscapedString3()
    {
        await TestAsync(""" " {|String:goo|}\r\u0065bar " """);
    }

    [Fact]
    public async Task TestEscapedString4()
    {
        await TestAsync(""" " {|String:C|}:\t{|String:ests |}" """);
    }

    [Fact]
    public async Task TestEscapedString5()
    {
        await TestAsync(""" {|String:@" C:\tests "|} """);
    }

    [Fact]
    public async Task TestEscapedString6()
    {
        await TestAsync(""" " {|String:C|}:\\{|String:tests |}" """);
    }

    [Fact]
    public async Task TestEscapedString7()
    {
        await TestAsync(""" " {|String:C|}:\\{|String:tests|}\\{|String:goo |}" """);
    }

    [Fact]
    public async Task TestEscapedString8()
    {
        await TestAsync(""" {|String:@" C:\tests\goo "|} """);
    }

    [Fact]
    public async Task TestEscapedString9()
    {
        await TestAsync(""" $" {|String:C|}:\\{|String:tests|}\\{|String:goo |}{0} {|String:C|}:\\{|String:tests|}\\{|String:bar |}" """);
    }

    [Fact]
    public async Task TestEscapedString10()
    {
        await TestAsync(""" $@"{|String: C:\tests\goo |}{0}{|String: C:\tests\bar |}" """);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1908466")]
    public async Task TestEscapedString11()
    {
        await TestAsync(""" " {|String:open telemetry for audits |}\n\t {|String:and |}0{|String:Tel table doesn't exist yet |}" """);
    }

    [Fact]
    public async Task TestIdentifier1()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier2()
    {
        await TestAsync("""
            record {|Identifier:C|}
            {
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier3()
    {
        await TestAsync("""
            record class {|Identifier:C|}
            {
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier4()
    {
        await TestAsync("""
            delegate void {|Identifier:C|}();
            """);
    }

    [Fact]
    public async Task TestIdentifier5()
    {
        await TestAsync("""
            enum {|Identifier:C|} { }
            """);
    }

    [Fact]
    public async Task TestIdentifier6()
    {
        await TestAsync("""
            enum {|Identifier:C|}
            {
                {|Identifier:D|}
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier7()
    {
        await TestAsync("""
            enum {|Identifier:C|}
            {
                {|Identifier:D|}, {|Identifier:E|}
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier8()
    {
        await TestAsync("""
            interface {|Identifier:C|} { }
            """);
    }

    [Fact]
    public async Task TestIdentifier9()
    {
        await TestAsync("""
            struct {|Identifier:C|} { }
            """);
    }

    [Fact]
    public async Task TestIdentifier10()
    {
        await TestAsync("""
            record struct {|Identifier:C|}() { }
            """);
    }

    [Fact]
    public async Task TestIdentifier11()
    {
        await TestAsync("""
            class {|Identifier:C|}<{|Identifier:T|}> { }
            """);
    }

    [Fact]
    public async Task TestIdentifier12()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                private int {|Identifier:X|};
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier13()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                private int {|Identifier:X|}, {|Identifier:Y|};
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier14()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                private const int {|Identifier:X|};
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier15()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                private const int {|Identifier:X|}, {|Identifier:Y|};
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier16()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                private int {|Identifier:X|} => 0;
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier17()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                private event Action {|Identifier:X|};
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier18()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                private event Action {|Identifier:X|}, {|Identifier:Y|};
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier19()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                private event Action {|Identifier:X|} { add { } remove { } }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier20()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
                    int {|Identifier:E|};
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier21()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
                    int {|Identifier:E|}, {|Identifier:F|};
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier22()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
            {|Identifier:E|}:
                    return;
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier23()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}(int {|Identifier:E|})
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier24()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}(int {|Identifier:E|})
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier25()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}(int {|Identifier:E|}, int {|Identifier:F|})
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier26()
    {
        await TestAsync("""
            static class {|Identifier:C|}
            {
                static void {|Identifier:D|}(this int {|Identifier:E|})
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier27()
    {
        await TestAsync("""
            namespace {|Identifier:C|}
            {
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier28()
    {
        await TestAsync("""
            namespace {|Identifier:C|}.{|Identifier:D|}
            {
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier29()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
                    for (int {|Identifier:E|} = 0; E < 10; E++)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier30()
    {
        await TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
                    Goo(out var {|Identifier:E|});
                }
            }
            """);
    }

    [Fact]
    public async Task TestIdentifier31()
    {
        await TestAsync("""
            class {|Identifier:C|}() { }
            """);
    }

    [Fact]
    public async Task TestIdentifier32()
    {
        await TestAsync("""
            struct {|Identifier:C|}() { }
            """);
    }

    [Fact]
    public async Task TestIdentifier33()
    {
        await TestAsync("""
            class {|Identifier:C|};
            """);
    }

    [Fact]
    public async Task TestIdentifier34()
    {
        await TestAsync("""
            struct {|Identifier:C|};
            """);
    }
}
