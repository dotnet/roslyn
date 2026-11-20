// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Settings;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;

/// <summary>
/// The indent size that VS reports in its client settings (Tools > Options) applies when the Razor file's .editorconfig
/// has none. LSP formatting requests only carry the tab size, and the dotnet format path has no client settings, so
/// these tests aren't in the shared <c>DocumentFormattingTest</c>.
/// </summary>
public class ClientIndentSizeFormattingTest(ITestOutputHelper testOutput) : DocumentFormattingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/86431")]
    public Task IndentsBySpacesUsingClientIndentSize()
    {
        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: false, TabSize: 4, IndentSize: 2));

        return RunFormattingTestAsync(
            input: """
                <div>
                <span class="a"
                id="b">Content</span>
                </div>
                @code {
                void M()
                {
                }
                }
                """,
            htmlFormatted: """
                <div>
                  <span class="a"
                        id="b">Content</span>
                </div>
                @code {
                void M()
                {
                }
                }
                """,
            expected: """
                <div>
                  <span class="a"
                        id="b">Content</span>
                </div>
                @code {
                  void M()
                  {
                  }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/86431")]
    public Task EditorConfigIndentSizeOverridesClientIndentSize()
    {
        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: false, TabSize: 4, IndentSize: 2));

        return RunFormattingTestAsync(
            input: """
                <div>
                <span class="a"
                id="b">Content</span>
                </div>
                @code {
                void M()
                {
                }
                }
                """,
            htmlFormatted: """
                <div>
                   <span class="a"
                         id="b">Content</span>
                </div>
                @code {
                void M()
                {
                }
                }
                """,
            expected: """
                <div>
                   <span class="a"
                         id="b">Content</span>
                </div>
                @code {
                   void M()
                   {
                   }
                }
                """,
            additionalFiles:
            [
                (".editorconfig", """
                    root = true

                    [*.razor]
                    indent_size = 3
                    """)
            ]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/86431")]
    public Task IndentsByTabsIgnoringClientIndentSize()
    {
        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: true, TabSize: 4, IndentSize: 2));

        return RunFormattingTestAsync(
            input: """
                <div>
                <span>Content</span>
                </div>
                @code {
                void M()
                {
                }
                }
                """,
            htmlFormatted: """
                <div>
                	<span>Content</span>
                </div>
                @code {
                void M()
                {
                }
                }
                """,
            expected: """
                <div>
                	<span>Content</span>
                </div>
                @code {
                	void M()
                	{
                	}
                }
                """,
            insertSpaces: false);
    }
}
