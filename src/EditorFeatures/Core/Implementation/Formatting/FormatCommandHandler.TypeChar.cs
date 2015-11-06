// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler
    {
        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler)
        {
            ExecuteCommand(args, nextHandler, CancellationToken.None);
        }

        private void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CancellationToken cancellationToken)
        {
            ExecuteReturnOrTypeCommand(args, nextHandler, cancellationToken);
        }

        private bool TryFormat(
            ITextView textView, Document document, IEditorFormattingService formattingService, char typedChar, int position, bool formatOnReturn, CancellationToken cancellationToken)
        {
            var changes = formatOnReturn
                ? formattingService.GetFormattingChangesOnReturnAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken)
                : formattingService.GetFormattingChangesAsync(document, typedChar, position, cancellationToken).WaitAndGetResult(cancellationToken);

            if (changes == null || changes.Count == 0)
            {
                return false;
            }

            using (var transaction = CreateEditTransaction(textView, EditorFeaturesResources.AutomaticFormatting))
            {
                transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance;
                document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, cancellationToken);
                transaction.Complete();
            }

            return true;
        }

        private CaretPreservingEditTransaction CreateEditTransaction(ITextView view, string description)
        {
            return new CaretPreservingEditTransaction(description, view, _undoHistoryRegistry, _editorOperationsFactoryService);
        }
    }
}
