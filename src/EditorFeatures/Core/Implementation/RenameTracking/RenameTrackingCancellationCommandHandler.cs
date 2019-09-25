// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [Name(PredefinedCommandHandlerNames.RenameTrackingCancellation)]
    [Order(After = PredefinedCommandHandlerNames.SignatureHelpBeforeCompletion)]
    [Order(After = PredefinedCommandHandlerNames.SignatureHelpAfterCompletion)]
    [Order(After = PredefinedCommandHandlerNames.AutomaticCompletion)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    [Order(After = PredefinedCommandHandlerNames.QuickInfo)]
    [Order(After = PredefinedCommandHandlerNames.EventHookup)]
    internal class RenameTrackingCancellationCommandHandler : VSCommanding.ICommandHandler<EscapeKeyCommandArgs>
    {
        [ImportingConstructor]
        public RenameTrackingCancellationCommandHandler()
        {
        }

        public string DisplayName => EditorFeaturesResources.Rename_Tracking_Cancellation;

        public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            return document != null &&
                RenameTrackingDismisser.DismissVisibleRenameTracking(document.Project.Solution.Workspace, document.Id);
        }

        public VSCommanding.CommandState GetCommandState(EscapeKeyCommandArgs args)
        {
            return VSCommanding.CommandState.Unspecified;
        }
    }
}
