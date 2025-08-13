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
public sealed class SpellCheckSpanTests : AbstractSpellCheckSpanTests
{
    protected override EditorTestWorkspace CreateWorkspace(string content)
        => EditorTestWorkspace.CreateCSharp(content);

    [Fact]
    public Task TestSingleLineComment1()
        => TestAsync("{|Comment:// Goo|}");

    [Fact]
    public Task TestSingleLineComment2()
        => TestAsync("""
            {|Comment:// Goo|}
            """);

    [Fact]
    public Task TestMultiLineComment1()
        => TestAsync("{|Comment:/* Goo */|}");

    [Fact]
    public Task TestMultiLineComment2()
        => TestAsync("""
            {|Comment:/*
               Goo
             */|}
            """);

    [Fact]
    public Task TestMultiLineComment3()
        => TestAsync("""
            {|Comment:/*
               Goo
             |}
            """);

    [Fact]
    public Task TestMultiLineComment4()
        => TestAsync("""
            {|Comment:/**/|}
            """);

    [Fact]
    public Task TestMultiLineComment5()
        => TestAsync("""
            {|Comment:/*/|}
            """);

    [Fact]
    public Task TestDocComment1()
        => TestAsync("""
            ///{|Comment:goo bar baz|}
            class {|Identifier:C|}
            {
            }
            """);

    [Fact]
    public Task TestDocComment2()
        => TestAsync("""
            ///{|Comment:goo bar baz|}
            ///{|Comment:goo bar baz|}
            class {|Identifier:C|}
            {
            }
            """);

    [Fact]
    public Task TestDocComment3()
        => TestAsync("""
            ///{|Comment: |}<summary>{|Comment: goo bar baz |}</summary>
            class {|Identifier:C|}
            {
            }
            """);

    [Fact]
    public Task TestString1()
        => TestAsync(@"{|String:"" goo ""|}");

    [Fact]
    public Task TestString2()
        => TestAsync("""
            " goo
            """);

    [Fact]
    public Task TestString3()
        => TestAsync("""
            {|String:" goo "|}
            """);

    [Fact]
    public Task TestString4()
        => TestAsync("""
            " goo
            """);

    [Fact]
    public Task TestString5()
        => TestAsync("""
            {|String:@" goo "|}
            """);

    [Fact]
    public Task TestString6()
        => TestAsync("""
            @" goo
            """);

    [Fact]
    public Task TestString7()
        => TestAsync(@"{|String:"""""" goo """"""|}");

    [Fact]
    public Task TestString8()
        => TestAsync(""""
            """ goo ""
            """");

    [Fact]
    public Task TestString9()
        => TestAsync(""""
            """ goo "
            """");

    [Fact]
    public Task TestString10()
        => TestAsync(""""
            """ goo
            """");

    [Fact]
    public Task TestString11()
        => TestAsync(""""
            {|String:"""
                goo 
                """|}
            """");

    [Fact]
    public Task TestString12()
        => TestAsync(""""
            """
                goo
                ""
            """");

    [Fact]
    public Task TestString13()
        => TestAsync(""""
            """
                goo
                "
            """");

    [Fact]
    public Task TestString14()
        => TestAsync(""""
            """
                goo
            """");

    [Fact]
    public Task TestString15()
        => TestAsync("""
            $"{|String: goo |}"
            """);

    [Fact]
    public Task TestString16()
        => TestAsync("""
            $"{|String: goo |}{0}{|String: bar |}"
            """);

    [Fact]
    public Task TestString17()
        => TestAsync(""""
            $"""{|String: goo |}{0}{|String: bar |}"""
            """");

    [Fact]
    public Task TestString18()
        => TestAsync(""""
            $"""{|String: goo |}{0:abcd}{|String: bar |}"""
            """");

    [Fact]
    public Task TestEscapedString1()
        => TestAsync(""" " {|String:goo|}\r\n{|String:bar |}" """);

    [Fact]
    public Task TestEscapedString2()
        => TestAsync(""" " {|String:goo|}\r\t{|String:bar |}" """);

    [Fact]
    public Task TestEscapedString3()
        => TestAsync(""" " {|String:goo|}\r\u0065bar " """);

    [Fact]
    public Task TestEscapedString4()
        => TestAsync(""" " {|String:C|}:\t{|String:ests |}" """);

    [Fact]
    public Task TestEscapedString5()
        => TestAsync(""" {|String:@" C:\tests "|} """);

    [Fact]
    public Task TestEscapedString6()
        => TestAsync(""" " {|String:C|}:\\{|String:tests |}" """);

    [Fact]
    public Task TestEscapedString7()
        => TestAsync(""" " {|String:C|}:\\{|String:tests|}\\{|String:goo |}" """);

    [Fact]
    public Task TestEscapedString8()
        => TestAsync(""" {|String:@" C:\tests\goo "|} """);

    [Fact]
    public Task TestEscapedString9()
        => TestAsync(""" $" {|String:C|}:\\{|String:tests|}\\{|String:goo |}{0} {|String:C|}:\\{|String:tests|}\\{|String:bar |}" """);

    [Fact]
    public Task TestEscapedString10()
        => TestAsync(""" $@"{|String: C:\tests\goo |}{0}{|String: C:\tests\bar |}" """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1908466")]
    public Task TestEscapedString11()
        => TestAsync(""" " {|String:open telemetry for audits |}\n\t {|String:and |}0{|String:Tel table doesn't exist yet |}" """);

    [Fact]
    public Task TestIdentifier1()
        => TestAsync("""
            class {|Identifier:C|}
            {
            }
            """);

    [Fact]
    public Task TestIdentifier2()
        => TestAsync("""
            record {|Identifier:C|}
            {
            }
            """);

    [Fact]
    public Task TestIdentifier3()
        => TestAsync("""
            record class {|Identifier:C|}
            {
            }
            """);

    [Fact]
    public Task TestIdentifier4()
        => TestAsync("""
            delegate void {|Identifier:C|}();
            """);

    [Fact]
    public Task TestIdentifier5()
        => TestAsync("""
            enum {|Identifier:C|} { }
            """);

    [Fact]
    public Task TestIdentifier6()
        => TestAsync("""
            enum {|Identifier:C|}
            {
                {|Identifier:D|}
            }
            """);

    [Fact]
    public Task TestIdentifier7()
        => TestAsync("""
            enum {|Identifier:C|}
            {
                {|Identifier:D|}, {|Identifier:E|}
            }
            """);

    [Fact]
    public Task TestIdentifier8()
        => TestAsync("""
            interface {|Identifier:C|} { }
            """);

    [Fact]
    public Task TestIdentifier9()
        => TestAsync("""
            struct {|Identifier:C|} { }
            """);

    [Fact]
    public Task TestIdentifier10()
        => TestAsync("""
            record struct {|Identifier:C|}() { }
            """);

    [Fact]
    public Task TestIdentifier11()
        => TestAsync("""
            class {|Identifier:C|}<{|Identifier:T|}> { }
            """);

    [Fact]
    public Task TestIdentifier12()
        => TestAsync("""
            class {|Identifier:C|}
            {
                private int {|Identifier:X|};
            }
            """);

    [Fact]
    public Task TestIdentifier13()
        => TestAsync("""
            class {|Identifier:C|}
            {
                private int {|Identifier:X|}, {|Identifier:Y|};
            }
            """);

    [Fact]
    public Task TestIdentifier14()
        => TestAsync("""
            class {|Identifier:C|}
            {
                private const int {|Identifier:X|};
            }
            """);

    [Fact]
    public Task TestIdentifier15()
        => TestAsync("""
            class {|Identifier:C|}
            {
                private const int {|Identifier:X|}, {|Identifier:Y|};
            }
            """);

    [Fact]
    public Task TestIdentifier16()
        => TestAsync("""
            class {|Identifier:C|}
            {
                private int {|Identifier:X|} => 0;
            }
            """);

    [Fact]
    public Task TestIdentifier17()
        => TestAsync("""
            class {|Identifier:C|}
            {
                private event Action {|Identifier:X|};
            }
            """);

    [Fact]
    public Task TestIdentifier18()
        => TestAsync("""
            class {|Identifier:C|}
            {
                private event Action {|Identifier:X|}, {|Identifier:Y|};
            }
            """);

    [Fact]
    public Task TestIdentifier19()
        => TestAsync("""
            class {|Identifier:C|}
            {
                private event Action {|Identifier:X|} { add { } remove { } }
            }
            """);

    [Fact]
    public Task TestIdentifier20()
        => TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
                    int {|Identifier:E|};
                }
            }
            """);

    [Fact]
    public Task TestIdentifier21()
        => TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
                    int {|Identifier:E|}, {|Identifier:F|};
                }
            }
            """);

    [Fact]
    public Task TestIdentifier22()
        => TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
            {|Identifier:E|}:
                    return;
                }
            }
            """);

    [Fact]
    public Task TestIdentifier23()
        => TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}(int {|Identifier:E|})
                {
                }
            }
            """);

    [Fact]
    public Task TestIdentifier24()
        => TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}(int {|Identifier:E|})
                {
                }
            }
            """);

    [Fact]
    public Task TestIdentifier25()
        => TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}(int {|Identifier:E|}, int {|Identifier:F|})
                {
                }
            }
            """);

    [Fact]
    public Task TestIdentifier26()
        => TestAsync("""
            static class {|Identifier:C|}
            {
                static void {|Identifier:D|}(this int {|Identifier:E|})
                {
                }
            }
            """);

    [Fact]
    public Task TestIdentifier27()
        => TestAsync("""
            namespace {|Identifier:C|}
            {
            }
            """);

    [Fact]
    public Task TestIdentifier28()
        => TestAsync("""
            namespace {|Identifier:C|}.{|Identifier:D|}
            {
            }
            """);

    [Fact]
    public Task TestIdentifier29()
        => TestAsync("""
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

    [Fact]
    public Task TestIdentifier30()
        => TestAsync("""
            class {|Identifier:C|}
            {
                void {|Identifier:D|}()
                {
                    Goo(out var {|Identifier:E|});
                }
            }
            """);

    [Fact]
    public Task TestIdentifier31()
        => TestAsync("""
            class {|Identifier:C|}() { }
            """);

    [Fact]
    public Task TestIdentifier32()
        => TestAsync("""
            struct {|Identifier:C|}() { }
            """);

    [Fact]
    public Task TestIdentifier33()
        => TestAsync("""
            class {|Identifier:C|};
            """);

    [Fact]
    public Task TestIdentifier34()
        => TestAsync("""
            struct {|Identifier:C|};
            """);
}
