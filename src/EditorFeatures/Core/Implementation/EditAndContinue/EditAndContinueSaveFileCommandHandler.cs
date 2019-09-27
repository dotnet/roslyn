// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Export]
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.EditAndContinueFileSave)]
    internal sealed class EditAndContinueSaveFileCommandHandler : IChainedCommandHandler<SaveCommandArgs>
    {
        private readonly Workspace _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueSaveFileCommandHandler(PrimaryWorkspace workspace)
        {
            _workspace = workspace.Workspace;
        }

        public string DisplayName => PredefinedCommandHandlerNames.EditAndContinueFileSave;

        void IChainedCommandHandler<SaveCommandArgs>.ExecuteCommand(SaveCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            var encService = _workspace.Services.GetService<IEditAndContinueWorkspaceService>();
            if (encService != null)
            {
                var documentId = _workspace.GetDocumentIdInCurrentContext(args.SubjectBuffer.AsTextContainer());
                if (documentId != null)
                {
                    encService.OnSourceFileUpdated(documentId);
                }
            }

            nextCommandHandler();
        }

        public VSCommanding.CommandState GetCommandState(SaveCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler)
            => nextCommandHandler();
    }
}


