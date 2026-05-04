// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel;
using Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests;

[UseExportProvider]
public sealed class EditorFactoryTests
{
    // Remove VisualStudioWorkspaceTelemetryService from the composition, since it tries to initialize the "telemetry" in the other process right away
    private static readonly TestComposition s_composition =
        CodeModelTestHelpers.Composition
            .AddParts(typeof(ThrowingRemoteHostClientProvider));

    [WpfFact]
    public async Task FormatDocumentCreatedFromTemplateAsync_DoesNotRequestRemoteHostClient()
    {
        // This test asserts that we don't have a request for the RemoteHost when trying to format a newly created document.
        // Since that can happen during the creation of a new solution, we can end up in cases where we are blocking the UI thread on the launch
        // of the remote host where we're then blocking on the spinning up of an entire process.

        using var tempRoot = new TempRoot();
        var filePath = tempRoot.CreateFile("Test", "cs").Path;
        var contents = """
            class C
            {
                void M()
                {
                }
            }
            """.ReplaceLineEndings();

        File.WriteAllText(filePath, contents);

        var exportProvider = s_composition.ExportProviderFactory.CreateExportProvider();
        using var workspace = exportProvider.GetExportedValue<MockVisualStudioWorkspace>();
        var remoteHostClientProvider = (ThrowingRemoteHostClientProvider)workspace.Services.GetRequiredService<IRemoteHostClientProvider>();
        var editorFactory = new CSharpEditorFactory(new MockComponentModel(exportProvider));

        await editorFactory.FormatDocumentCreatedFromTemplateAsync(Mock.Of<IVsHierarchy>(), filePath, CancellationToken.None);

        // Assert there were no requests that might have gotten caught by some other layer of the stack.
        Assert.Empty(remoteHostClientProvider.StackTracesOfAttemptsToGetTheRemoteHost);
    }

    [ExportWorkspaceService(typeof(IRemoteHostClientProvider), WorkspaceKind.Host), Shared, PartNotDiscoverable]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class ThrowingRemoteHostClientProvider() : IRemoteHostClientProvider
    {
        private readonly ConcurrentBag<StackTrace> _stackTracesOfAttemptsToGetTheRemoteHost = new ConcurrentBag<StackTrace>();

        public IEnumerable<StackTrace> StackTracesOfAttemptsToGetTheRemoteHost => _stackTracesOfAttemptsToGetTheRemoteHost;

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
        {
            // Keep track of all the places this gets called. This is done just to ensure we don't catch the failure somewhere and try to fall to an "in-process" path and run anwyays.
            _stackTracesOfAttemptsToGetTheRemoteHost.Add(new StackTrace());
            throw new XunitException("Unexpected attempt to request a remote host client while formatting a newly created document.");
        }

        public Task WaitForClientCreationAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
