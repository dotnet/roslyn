﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertBetweenRegularAndVerbatimString;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertBetweenRegularAndVerbatimString)]
public sealed class ConvertBetweenRegularAndVerbatimInterpolatedStringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new ConvertBetweenRegularAndVerbatimInterpolatedStringCodeRefactoringProvider();

    [Fact]
    public Task EmptyRegularString()
        => TestMissingAsync("""
            class Test
            {
                void Method()
                {
                    var v = $"[||]";
                }
            }
            """);

    [Fact]
    public Task RegularStringWithMissingCloseQuote()
        => TestMissingAsync("""
            class Test
            {
                void Method()
                {
                    var v = $"[||];
                }
            }
            """);

    [Fact]
    public Task VerbatimStringWithMissingCloseQuote()
        => TestMissingAsync("""
            class Test
            {
                void Method()
                {
                    var v = $@"[||];
                }
            }
            """);

    [Fact]
    public Task EmptyVerbatimString()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $@"[||]";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $"";
                }
            }
            """);

    [Fact]
    public Task TestLeadingAndTrailingTrivia()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v =
                        // leading
                        $@"[||]" /* trailing */;
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v =
                        // leading
                        $"" /* trailing */;
                }
            }
            """);

    [Fact]
    public Task RegularStringWithBasicText()
        => TestMissingAsync("""
            class Test
            {
                void Method()
                {
                    var v = $"[||]a";
                }
            }
            """);

    [Fact]
    public Task VerbatimStringWithBasicText()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $@"[||]a";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $"a";
                }
            }
            """);

    [Fact]
    public Task RegularStringWithUnicodeEscape()
        => TestMissingAsync("""
            class Test
            {
                void Method()
                {
                    var v = $"[||]\u0001";
                }
            }
            """);

    [Fact]
    public Task RegularStringWithEscapedNewLine()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $"[||]a\r\nb";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $@"a
            b";
                }
            }
            """);

    [Fact]
    public Task VerbatimStringWithNewLine()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $@"[||]a
            b";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $"a\r\nb";
                }
            }
            """);

    [Fact]
    public Task RegularStringWithEscapedNull()
        => TestMissingAsync("""
            class Test
            {
                void Method()
                {
                    var v = $"[||]a\0b";
                }
            }
            """);

    [Fact]
    public Task RegularStringWithEscapedQuote()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $"[||]a\"b";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $@"a""b";
                }
            }
            """);

    [Fact]
    public Task VerbatimStringWithEscapedQuote()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $@"[||]a""b";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $"a\"b";
                }
            }
            """);

    [Fact]
    public Task RegularStringWithEscapedQuoteAndMultipleParts()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $"[||]{1}\"{2}";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $@"{1}""{2}";
                }
            }
            """);

    [Fact]
    public Task VerbatimStringWithEscapedQuoteAndMultipleParts()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $@"[||]{1}""{2}";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $"{1}\"{2}";
                }
            }
            """);

    [Fact]
    public Task EscapedCurlyBracesInRegularString()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $"[||]a\r\n{{1}}";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $@"a
            {{1}}";
                }
            }
            """);

    [Fact]
    public Task EscapedCurlyBracesInVerbatimString()
        => TestInRegularAndScript1Async("""
            class Test
            {
                void Method()
                {
                    var v = $@"[||]a
            {{1}}";
                }
            }
            """,
            """
            class Test
            {
                void Method()
                {
                    var v = $"a\r\n{{1}}";
                }
            }
            """);
}
