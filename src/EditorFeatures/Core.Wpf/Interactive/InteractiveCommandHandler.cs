// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal abstract class InteractiveCommandHandler :
        ICommandHandler<ExecuteInInteractiveCommandArgs>,
        ICommandHandler<CopyToInteractiveCommandArgs>
    {
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        protected InteractiveCommandHandler(
            IContentTypeRegistryService contentTypeRegistryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _contentTypeRegistryService = contentTypeRegistryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        protected IContentTypeRegistryService ContentTypeRegistryService { get { return _contentTypeRegistryService; } }

        protected abstract IInteractiveWindow OpenInteractiveWindow(bool focus);

        protected abstract ISendToInteractiveSubmissionProvider SendToInteractiveSubmissionProvider { get; }

        public string DisplayName => EditorFeaturesResources.Interactive;

        private string GetSelectedText(EditorCommandArgs args, CancellationToken cancellationToken)
        {
            var editorOptions = _editorOptionsFactoryService.GetOptions(args.SubjectBuffer);
            return SendToInteractiveSubmissionProvider.GetSelectedText(editorOptions, args, cancellationToken);
        }

        CommandState ICommandHandler<ExecuteInInteractiveCommandArgs>.GetCommandState(ExecuteInInteractiveCommandArgs args)
            => CommandState.Available;

        bool ICommandHandler<ExecuteInInteractiveCommandArgs>.ExecuteCommand(ExecuteInInteractiveCommandArgs args, CommandExecutionContext context)
        {
            var window = OpenInteractiveWindow(focus: false);
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesWpfResources.Executing_selection_in_Interactive_Window))
            {
                var submission = GetSelectedText(args, context.OperationContext.UserCancellationToken);
                if (!string.IsNullOrWhiteSpace(submission))
                {
                    window.SubmitAsync(new string[] { submission });
                }
            }

            return true;
        }

        CommandState ICommandHandler<CopyToInteractiveCommandArgs>.GetCommandState(CopyToInteractiveCommandArgs args)
            => CommandState.Available;

        bool ICommandHandler<CopyToInteractiveCommandArgs>.ExecuteCommand(CopyToInteractiveCommandArgs args, CommandExecutionContext context)
        {
            var window = OpenInteractiveWindow(focus: true);
            var buffer = window.CurrentLanguageBuffer;

            if (buffer != null)
            {
                CopyToWindow(window, args, context);
            }
            else
            {
                Action action = null;
                action = new Action(() =>
                {
                    window.ReadyForInput -= action;
                    CopyToWindow(window, args, context);
                });

                window.ReadyForInput += action;
            }

            return true;
        }

        private void CopyToWindow(IInteractiveWindow window, CopyToInteractiveCommandArgs args, CommandExecutionContext context)
        {
            var buffer = window.CurrentLanguageBuffer;
            Debug.Assert(buffer != null);

            using (var edit = buffer.CreateEdit())
            using (var waitScope = context.OperationContext.AddScope(allowCancellation: true,
                EditorFeaturesWpfResources.Copying_selection_to_Interactive_Window))
            {
                var text = GetSelectedText(args, context.OperationContext.UserCancellationToken);

                // If the last line isn't empty in the existing submission buffer, we will prepend a
                // newline
                var lastLine = buffer.CurrentSnapshot.GetLineFromLineNumber(buffer.CurrentSnapshot.LineCount - 1);
                if (lastLine.Extent.Length > 0)
                {
                    var editorOptions = _editorOptionsFactoryService.GetOptions(args.SubjectBuffer);
                    text = editorOptions.GetNewLineCharacter() + text;
                }

                edit.Insert(buffer.CurrentSnapshot.Length, text);
                edit.Apply();
            }

            // Move the caret to the end
            var editorOperations = _editorOperationsFactoryService.GetEditorOperations(window.TextView);
            var endPoint = new VirtualSnapshotPoint(window.TextView.TextBuffer.CurrentSnapshot, window.TextView.TextBuffer.CurrentSnapshot.Length);
            editorOperations.SelectAndMoveCaret(endPoint, endPoint);
        }
    }
}
