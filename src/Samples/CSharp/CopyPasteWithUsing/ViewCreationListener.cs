// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Samples.CodeAction.CopyPasteWithUsing
{
    [Export(typeof(IVsTextViewCreationListener)), Shared]
    [ContentType("Roslyn C#")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class ViewCreationListener : IVsTextViewCreationListener
    {
        private readonly IVsEditorAdaptersFactoryService adaptersFactory;
        private readonly CopyDataService copyDataService;

        [ImportingConstructor]
        public ViewCreationListener(
            IVsEditorAdaptersFactoryService adaptersFactory,
            CopyDataService copyDataService)
        {
            this.adaptersFactory = adaptersFactory;
            this.copyDataService = copyDataService;
        }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            Contract.ThrowIfNull(textViewAdapter);

            var view = adaptersFactory.GetWpfTextView(textViewAdapter);
            Contract.ThrowIfNull(view, "Could not get IWpfTextView for IVsTextView");

            view.Closed += OnTextViewClosed;

            AttachToVsTextView(textViewAdapter, view);
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            var view = sender as IWpfTextView;
            if (view == null)
            {
                return;
            }

            view.Closed -= OnTextViewClosed;
        }

        private void AttachToVsTextView(IVsTextView shimView, IWpfTextView editorView)
        {
            // Add command filter to IVsTextView. If something goes wrong, throw.
            var commandFilter = new CommandFilter(this.copyDataService, editorView);

            var nextCommandTarget = default(IOleCommandTarget);
            int returnValue = shimView.AddCommandFilter(commandFilter, out nextCommandTarget);
            Marshal.ThrowExceptionForHR(returnValue);
            Contract.ThrowIfNull(nextCommandTarget);

            commandFilter.Next = nextCommandTarget;
        }

        private class CommandFilter : IOleCommandTarget
        {
            private readonly CopyDataService copyDataService;

            private readonly IWpfTextView view;
            private readonly PasteHandler pasteHandler;

            public CommandFilter(
                CopyDataService copyDataService,
                IWpfTextView view)
            {
                this.copyDataService = copyDataService;
                this.view = view;

                this.pasteHandler = new PasteHandler(this.view);
            }

            public int Exec(ref Guid pguidCmdGroup, uint commandID, uint executionInformatoin, IntPtr pvaIn, IntPtr pvaOut)
            {
                var copyCommand = IsCopyCommand(pguidCmdGroup, commandID);
                var pasteCommand = IsPasteCommand(pguidCmdGroup, commandID);

                // it doesn't have any command we are interested in
                if (!copyCommand && !pasteCommand)
                {
                    return this.Next.Exec(ref pguidCmdGroup, commandID, executionInformatoin, pvaIn, pvaOut);
                }

                // caret is not on right buffer
                var buffer = this.view.GetBufferContainingCaret();
                if (buffer == null)
                {
                    return this.Next.Exec(ref pguidCmdGroup, commandID, executionInformatoin, pvaIn, pvaOut);
                }

                if (copyCommand)
                {
                    // we could add support for cut as well.
                    SaveCopyData(buffer);
                }

                var applyPaste = false;
                if (pasteCommand)
                {
                    // check whether we can port usings
                    applyPaste = this.pasteHandler.CheckApplicable(buffer, this.copyDataService.Data);
                }

                var result = this.Next.Exec(ref pguidCmdGroup, commandID, executionInformatoin, pvaIn, pvaOut);

                if (applyPaste)
                {
                    // add usings
                    this.pasteHandler.Apply(buffer, this.copyDataService.Data);
                }

                return result;
            }

            private void SaveCopyData(ITextBuffer subjectBuffer)
            {
                var selection = view.Selection;
                var spans = selection.GetSnapshotSpansOnBuffer(subjectBuffer);
                if (spans.Count() != 1)
                {
                    return;
                }

                var document = subjectBuffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault();
                if (document == null)
                {
                    return;
                }

                if (document.SourceCodeKind != SourceCodeKind.Regular)
                {
                    return;
                }

                var span = spans.Select(s => new Microsoft.CodeAnalysis.Text.TextSpan(s.Start, s.Length)).First();
                if (span.IsEmpty)
                {
                    return;
                }

                var text = document.GetTextAsync().Result.ToString().Substring(span.Start, span.Length);

                var offsetMap = CopyData.CreateOffsetMap(document, span);

                this.copyDataService.SaveData(new CopyData(text, offsetMap));
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
            {
                // always delegate it to next command target
                return this.Next.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }

            private bool IsPasteCommand(Guid guidCmdGroup, uint commandID)
            {
                if (guidCmdGroup == VSConstants.VSStd2K &&
                    ((VSConstants.VSStd2KCmdID)commandID == VSConstants.VSStd2KCmdID.PASTE))
                {
                    return true;
                }

                if (guidCmdGroup == VSConstants.GUID_VSStandardCommandSet97 &&
                    ((VSConstants.VSStd97CmdID)commandID == VSConstants.VSStd97CmdID.Paste))
                {
                    return true;
                }

                return false;
            }

            private bool IsCopyCommand(Guid guidCmdGroup, uint commandID)
            {
                if (guidCmdGroup == VSConstants.GUID_VSStandardCommandSet97 &&
                    ((VSConstants.VSStd97CmdID)commandID == VSConstants.VSStd97CmdID.Copy))
                {
                    return true;
                }

                if (guidCmdGroup == VSConstants.VSStd2K &&
                    ((VSConstants.VSStd2KCmdID)commandID == VSConstants.VSStd2KCmdID.COPY))
                {
                    return true;
                }

                return false;
            }

            public IOleCommandTarget Next { get; set; }
        }
    }
}
