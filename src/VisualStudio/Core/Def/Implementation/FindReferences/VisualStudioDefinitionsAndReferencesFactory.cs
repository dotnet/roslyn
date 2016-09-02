using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using OLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    [ExportWorkspaceService(typeof(IDefinitionsAndReferencesFactory), ServiceLayer.Desktop), Shared]
    internal class VisualStudioDefinitionsAndReferencesFactory
        : DefaultDefinitionsAndReferencesFactory
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VisualStudioDefinitionsAndReferencesFactory(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override DefinitionItem GetThirdPartyDefinitionItem(
            Solution solution, ISymbol definition)
        {
            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();
            string filePath;
            int lineNumber, charOffset;
            if (!symbolNavigationService.WouldNavigateToSymbol(definition, solution, out filePath, out lineNumber, out charOffset))
            {
                return null;
            }

            var displayParts = GetDisplayParts(filePath, lineNumber, charOffset);
            return new ExternalDefinitionItem(
                GlyphTags.GetTags(definition.GetGlyph()),
                displayParts, _serviceProvider, filePath, lineNumber, charOffset);
        }

        private ImmutableArray<TaggedText> GetDisplayParts(
            string filePath, int lineNumber, int charOffset)
        {
            var builder = ImmutableArray.CreateBuilder<TaggedText>();

            var sourceLine = GetSourceLine(filePath, lineNumber).Trim(' ', '\t');

            // Put the line in 1-based for the presentation of this item.
            var formatted = $"{filePath} - ({lineNumber + 1}, {charOffset + 1}) : {sourceLine}";

            return ImmutableArray.Create(new TaggedText(TextTags.Text, formatted));
        }

        private string GetSourceLine(string filePath, int lineNumber)
        {
            using (var invisibleEditor = new InvisibleEditor(
                _serviceProvider, filePath, needsSave: false, needsUndoDisabled: false))
            {
                var vsTextLines = invisibleEditor.VsTextLines;
                int lineLength;
                string lineText;

                if (vsTextLines != null &&
                    vsTextLines.GetLengthOfLine(lineNumber, out lineLength) == VSConstants.S_OK &&
                    vsTextLines.GetLineText(lineNumber, 0, lineNumber, lineLength, out lineText) == VSConstants.S_OK)
                {
                    return lineText;
                }

                return ServicesVSResources.Preview_unavailable;
            }
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
                : base(tags, displayParts)
            {
                _serviceProvider = serviceProvider;
                _filePath = filePath;
                _lineNumber = lineNumber;
                _charOffset = charOffset;
            }

            public override bool CanNavigateTo() => true;

            public override bool TryNavigateTo()
            {
                return TryOpenFile() && TryNavigateToPosition();
            }

            private bool TryOpenFile()
            {
                var shellOpenDocument = (IVsUIShellOpenDocument)_serviceProvider.GetService(typeof(SVsUIShellOpenDocument));
                var textViewGuid = VSConstants.LOGVIEWID.TextView_guid;

                uint itemid;
                IVsUIHierarchy hierarchy;
                OLEServiceProvider oleServiceProvider;
                IVsWindowFrame frame;
                if (shellOpenDocument.OpenDocumentViaProject(
                    _filePath, ref textViewGuid, out oleServiceProvider, out hierarchy, out itemid, out frame) == VSConstants.S_OK)
                {
                    frame.Show();
                    return true;
                }

                return false;
            }

            private bool TryNavigateToPosition()
            {
                IVsRunningDocumentTable docTable = (IVsRunningDocumentTable)_serviceProvider.GetService(typeof(SVsRunningDocumentTable));

                IntPtr bufferPtr;
                IVsHierarchy hierarchy;
                uint cookie;
                uint itemid;

                if (docTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, _filePath, out hierarchy, out itemid, out bufferPtr, out cookie) != VSConstants.S_OK)
                {
                    return false;
                }

                try
                {
                    var lines = Marshal.GetObjectForIUnknown(bufferPtr) as IVsTextLines;
                    if (lines == null)
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
