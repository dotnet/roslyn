// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias InteractiveWindow;

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    // This command handler must be invoked after the handlers specified in `Order` attribute
    // (those handlers also implement `ICommandHandler<PasteCommandArgs>`),
    // because it will intercept the paste command and skip the rest of handlers in chain.  
    [ExportCommandHandler(PredefinedCommandHandlerNames.InteractivePaste, ContentTypeNames.RoslynContentType)]
    [Order(After = PredefinedCommandHandlerNames.Rename)]
    [Order(After = PredefinedCommandHandlerNames.FormatDocument)]
    [Order(After = PredefinedCommandHandlerNames.Commit)]
    [Order(After = PredefinedCommandHandlerNames.Completion)]
    internal sealed class InteractivePasteCommandHandler : ICommandHandler<PasteCommandArgs>
    {
        // Duplicated string, originally defined at `Microsoft.VisualStudio.InteractiveWindow.PredefinedInteractiveContentTypes`
        private const string InteractiveContentTypeName = "Interactive Content";
        // Duplicated string, originally defined at `Microsoft.VisualStudio.InteractiveWindow.InteractiveWindow`
        private const string InteractiveClipboardFormat = "89344A36-9821-495A-8255-99A63969F87D";

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

        [ImportingConstructor]
        public InteractivePasteCommandHandler(IEditorOperationsFactoryService editorOperationsFactoryService, ITextUndoHistoryRegistry textUndoHistoryRegistry)
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _textUndoHistoryRegistry = textUndoHistoryRegistry;
            RoslynClipboard = new SystemClipboardWrapper();
        }

        public void ExecuteCommand(PasteCommandArgs args, Action nextHandler)
        {
            // InteractiveWindow handles pasting by itself, which including checks for buffer types, etc.
            if (!args.TextView.TextBuffer.ContentType.IsOfType(InteractiveContentTypeName) &&
                RoslynClipboard.ContainsData(InteractiveClipboardFormat))
            {
                PasteInteractiveFormat(args.TextView);
            }
            else
            {
                nextHandler();
            }
        }

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // Avoid loading InteractiveWindow unless necessary
        private void PasteInteractiveFormat(ITextView textView)
        {
            var editorOperations = _editorOperationsFactoryService.GetEditorOperations(textView);

            var data = RoslynClipboard.GetDataObject();
            Debug.Assert(data != null);

            bool dataHasLineCutCopyTag = false;
            bool dataHasBoxCutCopyTag = false;

            dataHasLineCutCopyTag = data.GetDataPresent(ClipboardLineBasedCutCopyTag);
            dataHasBoxCutCopyTag = data.GetDataPresent(BoxSelectionCutCopyTag);
            Debug.Assert(!(dataHasLineCutCopyTag && dataHasBoxCutCopyTag));

            var blocks = InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.BufferBlock.Deserialize((string)RoslynClipboard.GetData(InteractiveClipboardFormat));

            var sb = PooledStringBuilder.GetInstance();
            foreach (var block in blocks)
            {
                switch (block.Kind)
                {
                    // the actual linebreak was converted to regular Input when copied
                    // This LineBreak block was created by coping box selection and is used as line separater when pasted
                    case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.LineBreak:
                        Debug.Assert(dataHasBoxCutCopyTag);
                        sb.Builder.Append(block.Content);
                        break;
                    case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.Input:
                    case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.Output:
                    case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.StandardInput:
                        sb.Builder.Append(block.Content);
                        break;
                }
            }
            var text = sb.ToStringAndFree();

            using (var transaction = _textUndoHistoryRegistry.GetHistory(textView.TextBuffer).CreateTransaction(EditorFeaturesResources.Paste))
            {
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
                        VirtualSnapshotPoint unusedStart, unusedEnd;
                        editorOperations.InsertTextAsBox(text, out unusedStart, out unusedEnd);
                    }
                }
                else
                {
                    editorOperations.InsertText(text);
                }
                editorOperations.AddAfterTextBufferChangePrimitive();
                transaction.Complete();
            }
        }

        private static bool HasNonWhiteSpaceCharacter(ITextSnapshotLine line)
        {
            var snapshot = line.Snapshot;
            int start = line.Start.Position;
            int count = line.Length;
            for (int i = 0; i < count; i++)
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
            {
                return Clipboard.ContainsData(format);
            }

            public object GetData(string format)
            {
                return Clipboard.GetData(format);
            }

            public IDataObject GetDataObject()
            {
                return Clipboard.GetDataObject();
            }
        }
    }
}