// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias InteractiveWindow;

using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
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
            var editorOperation = _editorOperationsFactoryService.GetEditorOperations(textView);
            var blocks = InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.BufferBlock.Deserialize((string)RoslynClipboard.GetData(InteractiveClipboardFormat));
            using (var transaction = _textUndoHistoryRegistry.GetHistory(textView.TextBuffer).CreateTransaction(EditorFeaturesResources.Paste))
            {
                foreach (var block in blocks)
                {
                    switch (block.Kind)
                    {
                        case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.Input:
                        case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.Output:
                        case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.StandardInput:
                            editorOperation.InsertText(block.Content);
                            break;
                    }
                }
                transaction.Complete();
            }
        }

        // The mock clipboard used in tests will implement this interface 
        internal interface IRoslynClipboard
        {
            bool ContainsData(string format);
            object GetData(string format);
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
        }
    }
}