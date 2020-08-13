// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Formatting
{
    [UseExportProvider]
    public class InferredIndentationTests
    {
        public static IEnumerable<object[]> LineEndings => new[] { new[] { "\r\n" }, new[] { "\n" } };

        [Theory]
        [MemberData(nameof(LineEndings))]
        public async Task BlankFileMatchesWorkspaceSettings(string lineEnding)
        {
            using var testWorkspace = CreateWithLines(
                lineEnding,
                "");
            var options = await testWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetOptionsAsync();

            Assert.Equal(FormattingOptions.NewLine.DefaultValue, options.GetOption(FormattingOptions.NewLine));
            Assert.Equal(FormattingOptions.UseTabs.DefaultValue, options.GetOption(FormattingOptions.UseTabs));
        }

        [Theory]
        [MemberData(nameof(LineEndings))]
        public async Task SingleLineWithTab(string lineEnding)
        {
            using var testWorkspace = CreateWithLines(
                lineEnding,
                "class C",
                "{",
                "\tvoid M() { }",
                "}");
            var options = await testWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetOptionsAsync();

            Assert.Equal(lineEnding, options.GetOption(FormattingOptions.NewLine));
            Assert.True(options.GetOption(FormattingOptions.UseTabs));
        }

        [Theory]
        [MemberData(nameof(LineEndings))]
        public async Task SingleLineWithFourSpaces(string lineEnding)
        {
            using var testWorkspace = CreateWithLines(
                lineEnding,
                "class C",
                "{",
                "    void M() { }",
                "}");
            var options = await testWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetOptionsAsync();

            Assert.Equal(lineEnding, options.GetOption(FormattingOptions.NewLine));
            Assert.False(options.GetOption(FormattingOptions.UseTabs));
            Assert.Equal(4, options.GetOption(FormattingOptions.IndentationSize));
        }

        private static TestWorkspace CreateWithLines(string lineEnding, params string[] lines)
        {
            var workspace = TestWorkspace.CreateCSharp(string.Join(lineEnding, lines), openDocuments: true);
            var editorOptionsFactoryService = workspace.ExportProvider.GetExportedValue<IEditorOptionsFactoryService>();

            editorOptionsFactoryService.GlobalOptions.SetOptionValue(DefaultOptions.AdaptiveFormattingOptionId, true);

            return workspace;
        }
    }
}
