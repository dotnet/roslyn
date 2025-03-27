// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.UnitTests;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.RemoteHost)]
public sealed class RemoteHostClientServiceFactoryTests
{
    private static readonly TestComposition s_composition = FeaturesTestCompositions.Features.WithTestHostParts(TestHost.OutOfProcess);

    private static AdhocWorkspace CreateWorkspace()
        => new(s_composition.GetHostServices());

    [Fact]
    public async Task UpdaterService()
    {
        using var workspace = CreateWorkspace();

        var exportProvider = workspace.Services.SolutionServices.ExportProvider;
        var listenerProvider = exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

        var checksumUpdater = new SolutionChecksumUpdater(workspace, listenerProvider, CancellationToken.None);
        var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();

        // make sure client is ready
        using var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

        // add solution, change document
        workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));
        var project = workspace.AddProject("proj", LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, "doc.cs", SourceText.From("code"));

        var oldText = document.GetTextSynchronously(CancellationToken.None);
        var newText = oldText.WithChanges([new TextChange(new TextSpan(0, 1), "abc")]);
        var newSolution = document.Project.Solution.WithDocumentText(document.Id, newText, PreservationMode.PreserveIdentity);

        workspace.TryApplyChanges(newSolution);

        // wait for listener
        var workspaceListener = listenerProvider.GetWaiter(FeatureAttribute.Workspace);
        await workspaceListener.ExpeditedWaitAsync();

        var listener = listenerProvider.GetWaiter(FeatureAttribute.SolutionChecksumUpdater);
        await listener.ExpeditedWaitAsync();

        // checksum should already exist
        Assert.True(workspace.CurrentSolution.CompilationState.TryGetStateChecksums(out _));

        checksumUpdater.Shutdown();
    }

    [Fact]
    public async Task TestSessionWithNoSolution()
    {
        using var workspace = CreateWorkspace();

        var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();

        var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

        using var connection = client.CreateConnection<IRemoteSymbolSearchUpdateService>(callbackTarget: null);
        Assert.True(await connection.TryInvokeAsync(
            (service, cancellationToken) => service.UpdateContinuouslyAsync("emptySource", Path.GetTempPath(), cancellationToken),
            CancellationToken.None));
    }

    private sealed class NullAssemblyAnalyzerLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
        {
            // doesn't matter what it returns
            return typeof(object).Assembly;
        }
    }
}
