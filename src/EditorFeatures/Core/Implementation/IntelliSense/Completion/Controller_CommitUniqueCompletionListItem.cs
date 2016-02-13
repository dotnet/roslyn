// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<CommitUniqueCompletionListItemCommandArgs>.GetCommandState(CommitUniqueCompletionListItemCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<CommitUniqueCompletionListItemCommandArgs>.ExecuteCommand(CommitUniqueCompletionListItemCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // User hit ctrl-space.  If there was no completion up then we want to trigger
                // completion. 
                var completionService = this.GetCompletionService();
                if (completionService == null)
                {
                    return;
                }

                if (!StartNewModelComputation(completionService, filterItems: true))
                {
                    return;
                }
            }

            // Get the selected item.  If it's unique, then we want to commit it.
            var model = this.sessionOpt.WaitForModel();
            if (model == null)
            {
                // Computation failed.  Just pass this command on.
                nextHandler();
                return;
            }

            // Note: Dev10 behavior seems to be that if there is no unique item that filtering is
            // turned off.  However, i do not know if this is desirable behavior, or merely a bug
            // with how that convoluted code worked.  So I'm not maintaining that behavior here.  If
            // we do want it through, it would be easy to get again simply by asking the model
            // computation to remove all filtering.

            if (model.IsUnique)
            {
                // We had a unique item in the list.  Commit it and dismiss this session.

                var selectedItem = Controller.GetExternallyUsableCompletionItem(model.SelectedItem);
                var textChange = GetCompletionRules().GetTextChange(selectedItem);
                this.Commit(selectedItem, textChange, model, null);
            }
        }
    }
}
