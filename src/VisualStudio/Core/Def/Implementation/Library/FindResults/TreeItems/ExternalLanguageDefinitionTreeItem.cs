// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using OLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class ExternalLanguageDefinitionTreeItem : AbstractTreeItem
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _filePath;
        private readonly int _lineNumber;
        private readonly int _charOffset;

        /// <summary>
        /// A definition from an external language service (e.g. xaml).
        /// </summary>
        public ExternalLanguageDefinitionTreeItem(string filePath, int lineNumber, int offset, string symbolName, ushort glyphIndex, IServiceProvider serviceProvider)
            : base(glyphIndex)
        {
            _filePath = filePath;
            _lineNumber = lineNumber;
            _charOffset = offset;
            _serviceProvider = serviceProvider;

            SetDisplayProperties(
                filePath,
                lineNumber,
                offset,
                offset,
                GetSourceLine(filePath, lineNumber, serviceProvider),
                symbolName.Length,
                projectNameDisambiguator: string.Empty);
        }

        private static string GetSourceLine(string filePath, int lineNumber, IServiceProvider serviceProvider)
        {
            using (var invisibleEditor = new InvisibleEditor(serviceProvider, filePath, needsSave: false, needsUndoDisabled: false))
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

                return ServicesVSResources.PreviewUnavailable;
            }
        }

        public override bool CanGoToDefinition()
        {
            return true;
        }

        public override int GoToSource()
        {
            return (TryOpenFile() && TryNavigateToPosition()) ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        private bool TryOpenFile()
        {
            var shellOpenDocument = (IVsUIShellOpenDocument)_serviceProvider.GetService(typeof(SVsUIShellOpenDocument));
            var textViewGuid = VSConstants.LOGVIEWID.TextView_guid;

            uint itemid;
            IVsUIHierarchy hierarchy;
            OLEServiceProvider oleServiceProvider;
            IVsWindowFrame frame;
            if (shellOpenDocument.OpenDocumentViaProject(_filePath, ref textViewGuid, out oleServiceProvider, out hierarchy, out itemid, out frame) == VSConstants.S_OK)
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
                    lines, VSConstants.LOGVIEWID.TextView_guid, _lineNumber, _charOffset, _lineNumber, _charOffset) == VSConstants.S_OK;
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
