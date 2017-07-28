﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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
            using (var analyzer = new VisualStudioAnalyzer(
                @"PathToAnalyzer",
                fileChangeService: mockFileChangeService.Object,
                hostDiagnosticUpdateSource: null,
                projectId: project.Id,
                workspace: workspace,
                loader: null,
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
                var serializer = new Serializer(workspace);

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
