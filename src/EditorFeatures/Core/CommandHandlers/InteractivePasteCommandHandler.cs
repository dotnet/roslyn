// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;         
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Interactive;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [ExportCommandHandler("InteractivePasteCommandHandler", ContentTypeNames.RoslynContentType)]
    [Order(After = PredefinedCommandHandlerNames.FormatDocument)]
    internal sealed class InteractivePasteCommandHandler : ICommandHandler<PasteCommandArgs>
    {
        // Originally defined at `Microsoft.VisualStudio.InteractiveWindow.PredefinedInteractiveContentTypes`
        private const string InteractiveContentTypeName = "Interactive Content";
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        public InteractivePasteCommandHandler(IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public void ExecuteCommand(PasteCommandArgs args, Action nextHandler)
        {
            if (args.TextView.TextBuffer.ContentType.TypeName != InteractiveContentTypeName &&
                Clipboard.ContainsData(ClipboardFormats.Interactive))
            {
                var editorOperation = _editorOperationsFactoryService.GetEditorOperations(args.TextView);
                var blocks = BufferBlock.Deserialize((string)Clipboard.GetData(ClipboardFormats.Interactive));
                // Paste each block separately.
                foreach (var block in blocks)
                {
                    switch (block.Kind)
                    {
                        case ReplSpanKind.Input:
                        case ReplSpanKind.Output:
                        case ReplSpanKind.StandardInput:
                            editorOperation.InsertText(block.Content);
                            break;
                    }
                }
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
    }
}
