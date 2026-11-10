// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;

public class DocumentRangeFormattingTest(ITestOutputHelper testOutput) : DocumentFormattingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5607")]
    public Task RangeFormatting_UsesEditorConfig_Razor()
        => RunFormattingTestAsync(
            input: """
                @code {
                    private void M(string [|value|])
                    {
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M(string value)
                    {
                    }
                }
                """,
            expected: """
                @code {
                    private void M(string value) {
                    }
                }
                """,
            validateHtmlFormattedMatchesWebTools: false,
            additionalFiles:
            [
                (".editorconfig", """
                    root = true

                    [*.razor]
                    csharp_new_line_before_open_brace = none
                    """)
            ]);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5607")]
    public Task RangeFormatting_UsesEditorConfig_Cshtml()
        => RunFormattingTestAsync(
            input: """
                @functions {
                    private void M(string [|value|])
                    {
                    }
                }
                """,
            htmlFormatted: """
                @functions {
                    private void M(string value)
                    {
                    }
                }
                """,
            expected: """
                @functions {
                    private void M(string value) {
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy,
            validateHtmlFormattedMatchesWebTools: false,
            additionalFiles:
            [
                (".editorconfig", """
                    root = true

                    [*.cshtml]
                    csharp_new_line_before_open_brace = none
                    """)
            ]);

    [Fact]
    public async Task RangeFormatOpenBrace()
    {
        await RunFormattingTestAsync(
            input: """
                <div></div>

                @code {
                    private void M(string [|request|]) {
                    }
                }
                """,
            htmlFormatted: """
                <div></div>
                
                @code {
                    private void M(string request) {
                    }
                }
                """,
            expected: """
                <div></div>
                
                @code {
                    private void M(string request)
                    {
                    }
                }
                """);
    }

    [Fact]
    public async Task RangeFormatOpenBrace_WithContent()
    {
        await RunFormattingTestAsync(
            input: """
                <div></div>

                @code {
                    private void M(string [|request|]) {
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                    }
                }
                """,
            htmlFormatted: """
                <div></div>
                
                @code {
                    private void M(string request) {
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                    }
                }
                """,
            expected: """
                <div></div>
                
                @code {
                    private void M(string request)
                    {
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                    }
                }
                """);
    }

    [Fact]
    public async Task FormatsSimpleHtmlTag_Range()
    {
        await RunFormattingTestAsync(
            input: """
                <html>
                <head>
                    <title>Hello</title>
                </head>
                <body>
                        [|<div>
                        </div>|]
                </body>
                </html>
                """,
            htmlFormatted: """
                <html>
                <head>
                    <title>Hello</title>
                </head>
                <body>
                    <div>
                    </div>
                </body>
                </html>
                """,
            expected: """
                <html>
                <head>
                    <title>Hello</title>
                </head>
                <body>
                    <div>
                    </div>
                </body>
                </html>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/29645")]
    public async Task FormatHtmlInIf_Range()
    {
        await RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    <p><em>Loading...</em></p>
                }
                else
                {
                    <table class="table">
                        <thead>
                            <tr>
                [|      <th>Date</th>
                        <th>Temp. (C)</th>
                        <th>Temp. (F)</th>
                        <th>Summary</th>|]
                            </tr>
                        </thead>
                    </table>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                <p><em>Loading...</em></p>
                }
                else
                {
                <table class="table">
                    <thead>
                        <tr>
                            <th>Date</th>
                            <th>Temp. (C)</th>
                            <th>Temp. (F)</th>
                            <th>Summary</th>
                        </tr>
                    </thead>
                </table>
                }
                """,
            expected: """
                @if (true)
                {
                    <p><em>Loading...</em></p>
                }
                else
                {
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Date</th>
                                <th>Temp. (C)</th>
                                <th>Temp. (F)</th>
                                <th>Summary</th>
                            </tr>
                        </thead>
                    </table>
                }
                """);
    }

    [Fact]
    public async Task RazorDiagnostics_SkipRangeFormatting()
    {
        await RunFormattingTestAsync(
            input: """
                @page "Goo"

                <div></div>

                [|<button|]
                @functions {
                 void M() { }
                }
                """,
            htmlFormatted: """
                @page "Goo"
                
                <div></div>
                
                <button @functions {
                        void M() { }
                        }
                """,
            expected: """
                @page "Goo"

                <div></div>

                <button
                @functions {
                 void M() { }
                }
                """,
            allowDiagnostics: true);
    }

    [Fact]
    public async Task RazorDiagnostics_SkipRangeFormatting_WholeDocumentRange()
    {
        await RunFormattingTestAsync(
            input: """
                [|<button
                @functions {
                 void M() { }
                }|]
                """,
            htmlFormatted: """
                <button @functions {
                        void M() { }
                        }
                """,
            expected: """
                <button
                @functions {
                 void M() { }
                }
                """,
            allowDiagnostics: true);
    }

    [Fact]
    public async Task RazorDiagnostics_DontSkipWhenOutsideOfRange()
    {
        await RunFormattingTestAsync(
            input: """
                @page "Goo"

                [|      <div></div>|]

                <button
                @functions {
                 void M() { }
                }
                """,
            htmlFormatted: """
                @page "Goo"
                
                <div></div>
                
                <button @functions {
                        void M() { }
                        }
                """,
            expected: """
                @page "Goo"

                <div></div>

                <button
                @functions {
                 void M() { }
                }
                """,
            allowDiagnostics: true);
    }

    [Fact]
    public async Task DoesNotFormat_CodeBlockDirective_NotInSelectedRange()
    {
        await RunFormattingTestAsync(
            input: """
                [|<div>Foo</div>|]
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """,
            htmlFormatted: """
                <div>Foo</div>
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """,
            expected: """
                <div>Foo</div>
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """);
    }

    [Fact]
    public async Task OnlyFormatsWithinRange()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{}
                        [|public interface Bar {
                }|]
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """,
            expected: """
                @functions {
                 public class Foo{}
                    public interface Bar
                    {
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11777")]
    public Task RangeFormat_AfterProperty()
        => RunFormattingTestAsync(
            input: """
                @code
                {
                    public string S
                    {
                        get => _s;
                        set
                        {
                            _s = value;
                        }
                    } [|private string _s = "";|]
                }
                """,
            htmlFormatted: """
                @code
                {
                    public string S
                    {
                        get => _s;
                        set
                        {
                            _s = value;
                        }
                    } private string _s = "";
                }
                """,
            expected: """
                @code
                {
                    public string S
                    {
                        get => _s;
                        set
                        {
                            _s = value;
                        }
                    }
                    private string _s = "";
                }
                """,
            debugAssertsEnabled: false
);
}
