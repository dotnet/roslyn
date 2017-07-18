using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Services
{
    public class VisualStudioSnapshotSerializationTests : SnapshotSerializationTestBase
    {
        [Fact, WorkItem(466282, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/466282")]
        public async Task TestUnresolvedAnalyzerReference()
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.CurrentSolution.AddProject("empty", "empty", LanguageNames.CSharp);
            var mockFileChangeService = new Mock<IVsFileChangeEx>();
            var analyzer = new VisualStudioAnalyzer(
                @"PathToAnalyzer",
                fileChangeService: mockFileChangeService.Object,
                hostDiagnosticUpdateSource: null,
                projectId: project.Id,
                workspace: workspace,
                loader: null,
                language: project.Language);
            project = project.WithAnalyzerReferences(new AnalyzerReference[]
            {
                analyzer.GetReference(),
            });

            var checksum = await project.State.GetChecksumAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(checksum);
        }
    }
}
