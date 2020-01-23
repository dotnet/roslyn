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
        [Fact]
        public async Task BlankFileMatchesWorkspaceSettings()
        {
            using var testWorkspace = CreateWithLines(
                "");
            var options = await testWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetOptionsAsync();

            Assert.Equal(FormattingOptions.UseTabs.DefaultValue, options.GetOption(FormattingOptions.UseTabs));
        }

        [Fact]
        public async Task SingleLineWithTab()
        {
            using var testWorkspace = CreateWithLines(
                "class C",
                "{",
                "\tvoid M() { }",
                "}");
            var options = await testWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetOptionsAsync();

            Assert.True(options.GetOption(FormattingOptions.UseTabs));
        }

        [Fact]
        public async Task SingleLineWithFourSpaces()
        {
            using var testWorkspace = CreateWithLines(
                "class C",
                "{",
                "    void M() { }",
                "}");
            var options = await testWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetOptionsAsync();

            Assert.False(options.GetOption(FormattingOptions.UseTabs));
            Assert.Equal(4, options.GetOption(FormattingOptions.IndentationSize));
        }

        private TestWorkspace CreateWithLines(params string[] lines)
        {
            var workspace = TestWorkspace.CreateCSharp(string.Join("\r\n", lines), openDocuments: true);
            var editorOptionsFactoryService = workspace.ExportProvider.GetExportedValue<IEditorOptionsFactoryService>();

            editorOptionsFactoryService.GlobalOptions.SetOptionValue(DefaultOptions.AdaptiveFormattingOptionId, true);

            return workspace;
        }
    }
}
