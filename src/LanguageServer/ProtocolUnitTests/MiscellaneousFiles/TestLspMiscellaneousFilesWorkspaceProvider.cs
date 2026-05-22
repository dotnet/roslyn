// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.MiscellaneousFiles;

[ExportCSharpVisualBasicLspServiceFactory(typeof(ILspMiscellaneousFilesWorkspaceProvider), WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
[PartNotDiscoverable]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TestLspMiscellaneousFilesWorkspaceProviderFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var hostServices = lspServices.GetRequiredService<HostServices>();
        return new TestLspMiscellaneousFilesWorkspaceProvider(hostServices);
    }

    private class TestLspMiscellaneousFilesWorkspaceProvider(HostServices host) : Workspace(host, WorkspaceKind.MiscellaneousFiles), ILspMiscellaneousFilesWorkspaceProvider
    {
        public ValueTask<TextDocument?> AddDocumentAsync(DocumentUri documentUri, TrackedDocumentInfo trackedDocumentInfo)
        {
            var documentFilePath = documentUri.GetDocumentFilePathFromUri();

            var sourceTextLoader = new SourceTextLoader(trackedDocumentInfo.SourceText, documentFilePath);

            var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
                this, documentFilePath, sourceTextLoader, new LanguageInformation(LanguageNames.CSharp, "csx"), trackedDocumentInfo.SourceText.ChecksumAlgorithm, Services.SolutionServices, [], false);
            OnProjectAdded(projectInfo);

            var id = projectInfo.Documents.Single().Id;
            return new(CurrentSolution.GetRequiredDocument(id));
        }

        public async ValueTask CloseDocumentAsync(DocumentUri uri)
        {
            await TryRemoveMiscellaneousDocumentAsync(uri);
        }

        public ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri)
        {
            // We'll only ever have a single document matching this URI in the misc solution.
            var matchingDocument = CurrentSolution.GetDocumentIds(uri).SingleOrDefault();
            if (matchingDocument != null)
            {
                var project = CurrentSolution.GetRequiredProject(matchingDocument.ProjectId);
                OnProjectRemoved(project.Id);

                return new(true);
            }

            return new(false);
        }
    }
}

