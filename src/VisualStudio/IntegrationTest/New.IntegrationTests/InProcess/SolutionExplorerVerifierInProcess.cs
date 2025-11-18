// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

[TestService]
internal sealed partial class SolutionExplorerVerifierInProcess
{
    public async Task ActiveDocumentIsSavedAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var activeDocument = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var editorAdaptersFactoryService = await GetComponentModelServiceAsync<IVsEditorAdaptersFactoryService>(cancellationToken);

        var runningDocumentTable = await GetRequiredGlobalServiceAsync<SVsRunningDocumentTable, IVsRunningDocumentTable4>(cancellationToken);

        foreach (var cookie in runningDocumentTable.GetRunningDocuments())
        {
            if (!runningDocumentTable.IsDocumentInitialized(cookie))
                continue;

            if (runningDocumentTable.GetDocumentData(cookie) is not IVsTextLines docData
                || editorAdaptersFactoryService.GetDocumentBuffer(docData) != activeDocument.TextDataModel.DocumentBuffer)
            {
                continue;
            }

            runningDocumentTable.UpdateDirtyState(cookie);
            if (!runningDocumentTable.IsDocumentDirty(cookie))
            {
                // We have verified the active document is not dirty
                return;
            }

            throw new InvalidOperationException($"Failed to save the active document: {runningDocumentTable.GetDocumentMoniker(cookie)}");
        }

        throw new InvalidOperationException("Failed to locate running document table cookie for the active document");
    }

    public async Task AllDocumentsAreSavedAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var runningDocumentTable = await GetRequiredGlobalServiceAsync<SVsRunningDocumentTable, IVsRunningDocumentTable4>(cancellationToken);

        var unsavedFiles = new List<string>();
        foreach (var cookie in runningDocumentTable.GetRunningDocuments())
        {
            runningDocumentTable.UpdateDirtyState(cookie);
            if (runningDocumentTable.IsDocumentDirty(cookie))
            {
                unsavedFiles.Add(runningDocumentTable.GetDocumentMoniker(cookie));
            }
        }

        AssertEx.EqualOrDiff("", string.Join(Environment.NewLine, unsavedFiles), "Unexpected dirty documents after failed Save All");
    }
}
