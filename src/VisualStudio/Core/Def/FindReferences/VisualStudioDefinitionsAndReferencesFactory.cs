// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences;

[ExportWorkspaceService(typeof(IExternalDefinitionItemProvider), ServiceLayer.Desktop), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioDefinitionsAndReferencesFactory(
    SVsServiceProvider serviceProvider,
    IThreadingContext threadingContext) : IExternalDefinitionItemProvider
{
    public async Task<DefinitionItem?> GetThirdPartyDefinitionItemAsync(
        Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken)
    {
        var symbolNavigationService = solution.Services.GetRequiredService<ISymbolNavigationService>();
        var result = await symbolNavigationService.GetExternalNavigationSymbolLocationAsync(definitionItem, cancellationToken).ConfigureAwait(false);
        if (result is not var (filePath, linePosition))
            return null;

        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var displayParts = GetDisplayParts_MustCallOnUIThread(filePath, linePosition);
        return new ExternalDefinitionItem(
            definitionItem.Tags, displayParts,
            serviceProvider, threadingContext,
            filePath, linePosition);
    }

    private ImmutableArray<TaggedText> GetDisplayParts_MustCallOnUIThread(
        string filePath, LinePosition linePosition)
    {
        var sourceLine = GetSourceLine_MustCallOnUIThread(filePath, linePosition.Line).Trim(' ', '\t');

        // Put the line in 1-based for the presentation of this item.
        var formatted = $"{filePath} - ({linePosition.Line + 1}, {linePosition.Character + 1}) : {sourceLine}";

        return [new TaggedText(TextTags.Text, formatted)];
    }

    private string GetSourceLine_MustCallOnUIThread(string filePath, int lineNumber)
    {
        using var invisibleEditor = new InvisibleEditor(
            serviceProvider, filePath, hierarchy: null, needsSave: false, needsUndoDisabled: false);
        var vsTextLines = invisibleEditor.VsTextLines;
        if (vsTextLines.GetLengthOfLine(lineNumber, out var lineLength) == VSConstants.S_OK &&
            vsTextLines.GetLineText(lineNumber, 0, lineNumber, lineLength, out var lineText) == VSConstants.S_OK)
        {
            return lineText;
        }

        return ServicesVSResources.Preview_unavailable;
    }

    private sealed class ExternalDefinitionItem(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        IServiceProvider serviceProvider,
        IThreadingContext threadingContext,
        string filePath,
        LinePosition linePosition)
        : DefinitionItem(
            tags,
            displayParts,
            nameDisplayParts: [],
            sourceSpans: default,
            metadataLocations: default,
            classifiedSpans: default,
            properties: null,
            displayableProperties: [],
            displayIfNoReferences: true)
    {
        internal override bool IsExternal => true;

        public override async Task<INavigableLocation?> GetNavigableLocationAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            return new NavigableLocation(async (options, cancellationToken) =>
            {
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                return TryOpenFile() && TryNavigateToPosition();
            });
        }

        private bool TryOpenFile()
        {
            var shellOpenDocument = (IVsUIShellOpenDocument)serviceProvider.GetService(typeof(SVsUIShellOpenDocument));
            var textViewGuid = VSConstants.LOGVIEWID.TextView_guid;
            if (shellOpenDocument.OpenDocumentViaProject(
                    filePath, ref textViewGuid, out _,
                    out _, out _, out var frame) == VSConstants.S_OK)
            {
                frame.Show();
                return true;
            }

            return false;
        }

        private bool TryNavigateToPosition()
        {
            var docTable = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            if (docTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, filePath,
                    out _, out _, out var bufferPtr, out _) != VSConstants.S_OK)
            {
                return false;
            }

            try
            {
                if (Marshal.GetObjectForIUnknown(bufferPtr) is not IVsTextLines lines)
                {
                    return false;
                }

                var textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));
                if (textManager == null)
                {
                    return false;
                }

                return textManager.NavigateToLineAndColumn(
                    lines, VSConstants.LOGVIEWID.TextView_guid,
                    linePosition.Line, linePosition.Character,
                    linePosition.Line, linePosition.Character) == VSConstants.S_OK;
            }
            finally
            {
                if (bufferPtr != IntPtr.Zero)
                {
                    Marshal.Release(bufferPtr);
                }
            }
        }
    }
}
