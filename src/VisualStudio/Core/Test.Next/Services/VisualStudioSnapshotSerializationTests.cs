using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Services
{
    [UseExportProvider]
    public class VisualStudioSnapshotSerializationTests : SnapshotSerializationTestBase
    {
        [Fact, WorkItem(466282, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/466282")]
        public async Task TestUnresolvedAnalyzerReference()
        {
            using (var workspace = new TestWorkspace())
            {
                var project = workspace.CurrentSolution.AddProject("empty", "empty", LanguageNames.CSharp);
                using (var analyzer = new VisualStudioAnalyzer(
                    @"PathToAnalyzer",
                    hostDiagnosticUpdateSource: null,
                    projectId: project.Id,
                    workspace: workspace,
                    language: project.Language))
                {
                    var analyzerReference = analyzer.GetReference();
                    project = project.WithAnalyzerReferences(new AnalyzerReference[]
                    {
                        analyzerReference,
                    });

                    var checksum = await project.State.GetChecksumAsync(CancellationToken.None).ConfigureAwait(false);
                    Assert.NotNull(checksum);

                    var assetBuilder = new CustomAssetBuilder(workspace);
                    var serializer = workspace.Services.GetService<ISerializerService>();

                    var asset = assetBuilder.Build(analyzerReference, CancellationToken.None);

                    using (var stream = SerializableBytes.CreateWritableStream())
                    using (var writer = new ObjectWriter(stream))
                    {
                        await asset.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                        stream.Position = 0;
                        using (var reader = ObjectReader.TryGetReader(stream))
                        {
                            var recovered = serializer.Deserialize<AnalyzerReference>(asset.Kind, reader, CancellationToken.None);
                            var assetFromStorage = assetBuilder.Build(recovered, CancellationToken.None);

                            Assert.Equal(asset.Checksum, assetFromStorage.Checksum);

                            // This won't round trip, but we should get an UnresolvedAnalyzerReference, with the same path
                            Assert.Equal(analyzerReference.FullPath, recovered.FullPath);
                        }
                    }
                }
            }
        }
    }
}
