// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias InteractiveWindow;

using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [ExportCommandHandler("InteractivePasteCommandHandler", ContentTypeNames.RoslynContentType)]
    [Order(After = PredefinedCommandHandlerNames.FormatDocument)]
    internal sealed class InteractivePasteCommandHandler : ICommandHandler<PasteCommandArgs>
    {
        // Duplicated string, originally defined at `Microsoft.VisualStudio.InteractiveWindow.PredefinedInteractiveContentTypes`
        private const string InteractiveContentTypeName = "Interactive Content";
        // Duplicated string, originally defined at `Microsoft.VisualStudio.InteractiveWindow.InteractiveWindow`
        private const string ClipboardFormat = "89344A36-9821-495A-8255-99A63969F87D";

        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        public InteractivePasteCommandHandler(IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public void ExecuteCommand(PasteCommandArgs args, Action nextHandler)
        {
            // InteractiveWindow handles pasting by itself, which including checks for buffer types, etc.
            if (!args.TextView.TextBuffer.ContentType.IsOfType(InteractiveContentTypeName) &&
                Clipboard.ContainsData(ClipboardFormat))
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
            var blocks = InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.BufferBlock.Deserialize((string)Clipboard.GetData(ClipboardFormat));
            var sb = new StringBuilder();
            foreach (var block in blocks)
            {
                switch (block.Kind)
                {
                    case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.Input:
                    case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.Output:
                    case InteractiveWindow::Microsoft.VisualStudio.InteractiveWindow.ReplSpanKind.StandardInput:
                        sb.Append(block.Content);
                        break;
                }
            }
            editorOperation.InsertText(sb.ToString());
        }
    }
}