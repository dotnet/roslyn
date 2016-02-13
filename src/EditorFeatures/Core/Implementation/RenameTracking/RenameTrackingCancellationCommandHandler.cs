// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.RenameTrackingCancellation, ContentTypeNames.RoslynContentType)]
    [Order(After = PredefinedCommandHandlerNames.SignatureHelp)]
    [Order(After = PredefinedCommandHandlerNames.IntelliSense)]
    [Order(After = PredefinedCommandHandlerNames.AutomaticCompletion)]
    [Order(After = PredefinedCommandHandlerNames.Completion)]
    [Order(After = PredefinedCommandHandlerNames.QuickInfo)]
    [Order(After = PredefinedCommandHandlerNames.EventHookup)]
    internal class RenameTrackingCancellationCommandHandler : ICommandHandler<EscapeKeyCommandArgs>
    {
        public void ExecuteCommand(EscapeKeyCommandArgs args, Action nextHandler)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document != null &&
                RenameTrackingDismisser.DismissVisibleRenameTracking(document.Project.Solution.Workspace, document.Id))
            {
                return;
            }

            nextHandler();
        }

        public CommandState GetCommandState(EscapeKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }
    }
}
