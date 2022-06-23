// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    // This command handler must be invoked after the handlers specified in `Order` attribute
    // (those handlers also implement `ICommandHandler<PasteCommandArgs>`),
    // because it will intercept the paste command and skip the rest of handlers in chain.  
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.InteractivePaste)]
    [Order(After = PredefinedCommandHandlerNames.Rename)]
    [Order(After = PredefinedCommandHandlerNames.FormatDocument)]
    [Order(After = PredefinedCommandHandlerNames.Commit)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal sealed class InteractivePasteCommandHandler : ICommandHandler<PasteCommandArgs>
    {
        // The following two field definitions have to stay in sync with VS editor implementation

        /// <summary>
        /// A data format used to tag the contents of the clipboard so that it's clear
        /// the data has been put in the clipboard by our editor
        /// </summary>
        internal const string ClipboardLineBasedCutCopyTag = "VisualStudioEditorOperationsLineCutCopyClipboardTag";

        /// <summary>
        /// A data format used to tag the contents of the clipboard as a box selection.
        /// This is the same string that was used in VS9 and previous versions.
        /// </summary>
        internal const string BoxSelectionCutCopyTag = "MSDEVColumnSelect";

        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;

        // This is for unit test purpose only, do not explicitly set this field otherwise.
        internal IRoslynClipboard RoslynClipboard;

        public string DisplayName => EditorFeaturesResources.Paste_in_Interactive;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractivePasteCommandHandler(IEditorOperationsFactoryService editorOperationsFactoryService, ITextUndoHistoryRegistry textUndoHistoryRegistry)
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _textUndoHistoryRegistry = textUndoHistoryRegistry;
            RoslynClipboard = new SystemClipboardWrapper();
        }

        public bool ExecuteCommand(PasteCommandArgs args, CommandExecutionContext context)
        {
            // InteractiveWindow handles pasting by itself, which including checks for buffer types, etc.
            if (!args.TextView.TextBuffer.ContentType.IsOfType(PredefinedInteractiveContentTypes.InteractiveContentTypeName) &&
                RoslynClipboard.ContainsData(InteractiveClipboardFormat.Tag))
            {
                PasteInteractiveFormat(args.TextView);
                return true;
            }
            else
            {
                return false;
            }
        }

        public CommandState GetCommandState(PasteCommandArgs args)
            => CommandState.Unspecified;

        [MethodImpl(MethodImplOptions.NoInlining)]  // Avoid loading InteractiveWindow unless necessary
        private void PasteInteractiveFormat(ITextView textView)
        {
            var editorOperations = _editorOperationsFactoryService.GetEditorOperations(textView);

            var data = RoslynClipboard.GetDataObject();
            Debug.Assert(data != null);

            var dataHasLineCutCopyTag = data.GetDataPresent(ClipboardLineBasedCutCopyTag);
            var dataHasBoxCutCopyTag = data.GetDataPresent(BoxSelectionCutCopyTag);
            Debug.Assert(!(dataHasLineCutCopyTag && dataHasBoxCutCopyTag));

            string text;
            try
            {
                text = InteractiveClipboardFormat.Deserialize(RoslynClipboard.GetData(InteractiveClipboardFormat.Tag));
            }
            catch (InvalidDataException)
            {
                text = "<bad clipboard data>";
            }

            using var transaction = _textUndoHistoryRegistry.GetHistory(textView.TextBuffer).CreateTransaction(EditorFeaturesResources.Paste);
            editorOperations.AddBeforeTextBufferChangePrimitive();
            if (dataHasLineCutCopyTag && textView.Selection.IsEmpty)
            {
                editorOperations.MoveToStartOfLine(extendSelection: false);
                editorOperations.InsertText(text);
            }
            else if (dataHasBoxCutCopyTag)
            {
                // If the caret is on a blank line, treat this like a normal stream insertion
                if (textView.Selection.IsEmpty && !HasNonWhiteSpaceCharacter(textView.Caret.Position.BufferPosition.GetContainingLine()))
                {
                    // trim the last newline before paste
                    var trimmed = text.Remove(text.LastIndexOf(textView.Options.GetNewLineCharacter()));
                    editorOperations.InsertText(trimmed);
                }
                else
                {
                    editorOperations.InsertTextAsBox(text, out _, out _);
                }
            }
            else
            {
                editorOperations.InsertText(text);
            }

            editorOperations.AddAfterTextBufferChangePrimitive();
            transaction.Complete();
        }

        private static bool HasNonWhiteSpaceCharacter(ITextSnapshotLine line)
        {
            var snapshot = line.Snapshot;
            var start = line.Start.Position;
            var count = line.Length;
            for (var i = 0; i < count; i++)
            {
                if (!char.IsWhiteSpace(snapshot[start + i]))
                {
                    return true;
                }
            }

            return false;
        }

        // The mock clipboard used in tests will implement this interface 
        internal interface IRoslynClipboard
        {
            bool ContainsData(string format);
            object GetData(string format);
            IDataObject GetDataObject();
        }

        // In product code, we use this simple wrapper around system clipboard.
        // Maybe at some point we can elevate this class and interface so they could be shared among Roslyn code base.
        private class SystemClipboardWrapper : IRoslynClipboard
        {
            public bool ContainsData(string format)
                => Clipboard.ContainsData(format);

            public object GetData(string format)
                => Clipboard.GetData(format);

            public IDataObject GetDataObject()
                => Clipboard.GetDataObject();
        }
    }
}
