﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [ExportWorkspaceService(typeof(IDefinitionsAndReferencesFactory), ServiceLayer.Desktop), Shared]
    internal class VisualStudioDefinitionsAndReferencesFactory
        : DefaultDefinitionsAndReferencesFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDefinitionsAndReferencesFactory(
            SVsServiceProvider serviceProvider,
            IThreadingContext threadingContext)
        {
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;
        }

        public override async Task<DefinitionItem?> GetThirdPartyDefinitionItemAsync(
            Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken)
        {
            var symbolNavigationService = solution.Workspace.Services.GetRequiredService<ISymbolNavigationService>();
            var result = await symbolNavigationService.WouldNavigateToSymbolAsync(definitionItem, cancellationToken).ConfigureAwait(false);
            if (result is not var (filePath, lineNumber, charOffset))
                return null;

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var displayParts = GetDisplayParts_MustCallOnUIThread(filePath, lineNumber, charOffset);
            return new ExternalDefinitionItem(
                definitionItem.Tags, displayParts,
                _serviceProvider, _threadingContext,
                filePath, lineNumber, charOffset);
        }

        private ImmutableArray<TaggedText> GetDisplayParts_MustCallOnUIThread(
            string filePath, int lineNumber, int charOffset)
        {
            var sourceLine = GetSourceLine_MustCallOnUIThread(filePath, lineNumber).Trim(' ', '\t');

            // Put the line in 1-based for the presentation of this item.
            var formatted = $"{filePath} - ({lineNumber + 1}, {charOffset + 1}) : {sourceLine}";

            return ImmutableArray.Create(new TaggedText(TextTags.Text, formatted));
        }

        private string GetSourceLine_MustCallOnUIThread(string filePath, int lineNumber)
        {
            using var invisibleEditor = new InvisibleEditor(
                _serviceProvider, filePath, hierarchy: null, needsSave: false, needsUndoDisabled: false);
            var vsTextLines = invisibleEditor.VsTextLines;
            if (vsTextLines.GetLengthOfLine(lineNumber, out var lineLength) == VSConstants.S_OK &&
                vsTextLines.GetLineText(lineNumber, 0, lineNumber, lineLength, out var lineText) == VSConstants.S_OK)
            {
                return lineText;
            }

            return ServicesVSResources.Preview_unavailable;
        }

        private class ExternalDefinitionItem : DefinitionItem
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly IThreadingContext _threadingContext;
            private readonly string _filePath;
            private readonly int _lineNumber;
            private readonly int _charOffset;

            internal override bool IsExternal => true;

            public ExternalDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                IServiceProvider serviceProvider,
                IThreadingContext threadingContext,
                string filePath,
                int lineNumber,
                int charOffset)
                : base(tags,
                       displayParts,
                       nameDisplayParts: ImmutableArray<TaggedText>.Empty,
                       originationParts: default,
                       sourceSpans: default,
                       properties: null,
                       displayableProperties: null,
                       displayIfNoReferences: true)
            {
                _serviceProvider = serviceProvider;
                _threadingContext = threadingContext;
                _filePath = filePath;
                _lineNumber = lineNumber;
                _charOffset = charOffset;
            }

            public override Task<bool> CanNavigateToAsync(Workspace workspace, CancellationToken cancellationToken)
                => SpecializedTasks.True;

            public override async Task<bool> TryNavigateToAsync(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                return TryOpenFile() && TryNavigateToPosition();
            }

            [Obsolete]
            public override bool CanNavigateTo(Workspace workspace, CancellationToken cancellationToken)
                => throw ExceptionUtilities.Unreachable;

            [Obsolete]
            public override bool TryNavigateTo(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
                => throw ExceptionUtilities.Unreachable;

            private bool TryOpenFile()
            {
                var shellOpenDocument = (IVsUIShellOpenDocument)_serviceProvider.GetService(typeof(SVsUIShellOpenDocument));
                var textViewGuid = VSConstants.LOGVIEWID.TextView_guid;
                if (shellOpenDocument.OpenDocumentViaProject(
                        _filePath, ref textViewGuid, out _,
                        out _, out _, out var frame) == VSConstants.S_OK)
                {
                    frame.Show();
                    return true;
                }

                return false;
            }

            private bool TryNavigateToPosition()
            {
                var docTable = (IVsRunningDocumentTable)_serviceProvider.GetService(typeof(SVsRunningDocumentTable));
                if (docTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, _filePath,
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

                    var textManager = (IVsTextManager)_serviceProvider.GetService(typeof(SVsTextManager));
                    if (textManager == null)
                    {
                        return false;
                    }

                    return textManager.NavigateToLineAndColumn(
                        lines, VSConstants.LOGVIEWID.TextView_guid,
                        _lineNumber, _charOffset,
                        _lineNumber, _charOffset) == VSConstants.S_OK;
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
}
