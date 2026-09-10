// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Remote;

public class RemoteWorkspaceProviderTest
{
    [Theory]
    [InlineData("cline-diff:Index.cshtml?SGVsbG8=", true)]
    [InlineData("custom://workspace/Component.razor?SGVsbG8=", true)]
    [InlineData("cline-diff:Program.cs?SGVsbG8=", false)]
    [InlineData("custom://workspace/README.md?SGVsbG8=", false)]
    [InlineData("custom://workspace/Component.razor#fragment", true)]
    [InlineData("custom://workspace/README.md#fragment", false)]
    [InlineData("Index.cshtml", true)]
    [InlineData("Program.cs", false)]
    [InlineData(@"C:\Project\Index.cshtml", true)]
    [InlineData("/Project/Component.razor", true)]
    [InlineData(@"C:\Project\Program.cs", false)]
    [InlineData("/Project/README.md", false)]
    public void IsRazorDocument(string filePath, bool expected)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Test", LanguageNames.CSharp);
        var document = project.AddAdditionalDocument(filePath, SourceText.From(""), filePath: filePath);

        Assert.Equal(filePath, document.FilePath);
        Assert.Equal(expected, document.IsRazorDocument());
        Assert.Equal(expected, document.Project.ContainsRazorDocuments());
    }

    [Fact]
    public async Task InitializeRemoteExportProviderBuilderAsync_OnlyInitializesOnce()
    {
        var callCount = 0;
        var initializationStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowInitializationToComplete = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        RemoteWorkspaceProvider.TestAccessor.ResetInitializeRemoteExportProviderBuilder();

        try
        {
            RemoteWorkspaceProvider.TestAccessor.SetInitializeRemoteExportProviderBuilder(async (_, _, _) =>
            {
                Interlocked.Increment(ref callCount);
                initializationStarted.TrySetResult(null);
                await allowInitializationToComplete.Task.ConfigureAwait(false);
                return "test-error";
            });

            var traceSource = new TraceSource(nameof(RemoteWorkspaceProviderTest));
            var firstTask = RemoteWorkspaceProvider.TestAccessor.InitializeRemoteExportProviderBuilderAsync("test", traceSource, CancellationToken.None);

            await initializationStarted.Task;

            var secondTask = RemoteWorkspaceProvider.TestAccessor.InitializeRemoteExportProviderBuilderAsync("test", traceSource, CancellationToken.None);
            Assert.False(secondTask.IsCompleted);

            allowInitializationToComplete.TrySetResult(null);

            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.Equal(1, callCount);
            Assert.Collection(results,
                result => Assert.Equal("test-error", result),
                result => Assert.Equal("test-error", result));
        }
        finally
        {
            RemoteWorkspaceProvider.TestAccessor.ResetInitializeRemoteExportProviderBuilder();
        }
    }
}
