// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [ExportWorkspaceService(typeof(IDefinitionsAndReferencesFactory), ServiceLayer.Desktop), Shared]
    internal class VisualStudioDefinitionsAndReferencesFactory
        : DefaultDefinitionsAndReferencesFactory
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDefinitionsAndReferencesFactory(SVsServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public override DefinitionItem GetThirdPartyDefinitionItem(
            Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken)
        {
            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();
            if (!symbolNavigationService.WouldNavigateToSymbol(
                    definitionItem, solution, cancellationToken,
                    out var filePath, out var lineNumber, out var charOffset))
            {
                return null;
            }

            var displayParts = GetDisplayParts(filePath, lineNumber, charOffset);
            return new ExternalDefinitionItem(
                definitionItem.Tags, displayParts,
                _serviceProvider, filePath, lineNumber, charOffset);
        }

        private ImmutableArray<TaggedText> GetDisplayParts(
            string filePath, int lineNumber, int charOffset)
        {
            var sourceLine = GetSourceLine(filePath, lineNumber).Trim(' ', '\t');

            // Put the line in 1-based for the presentation of this item.
            var formatted = $"{filePath} - ({lineNumber + 1}, {charOffset + 1}) : {sourceLine}";

            return ImmutableArray.Create(new TaggedText(TextTags.Text, formatted));
        }

        private string GetSourceLine(string filePath, int lineNumber)
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
            private readonly string _filePath;
            private readonly int _lineNumber;
            private readonly int _charOffset;

            internal override bool IsExternal => true;

            public ExternalDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                IServiceProvider serviceProvider,
                string filePath,
                int lineNumber,
                int charOffset)
                : base(tags, displayParts, ImmutableArray<TaggedText>.Empty,
                       originationParts: default,
                       sourceSpans: default,
                       properties: null,
                       displayableProperties: null,
                       displayIfNoReferences: true)
            {
                _serviceProvider = serviceProvider;
                _filePath = filePath;
                _lineNumber = lineNumber;
                _charOffset = charOffset;
            }

            public override bool CanNavigateTo(Workspace workspace) => true;

            public override bool TryNavigateTo(Workspace workspace, bool showInPreviewTab, bool activateTab)
                => TryOpenFile() && TryNavigateToPosition();

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
                    if (!(Marshal.GetObjectForIUnknown(bufferPtr) is IVsTextLines lines))
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
