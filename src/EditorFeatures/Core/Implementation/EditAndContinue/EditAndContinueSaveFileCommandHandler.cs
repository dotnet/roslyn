﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueSaveFileCommandHandler()
        {
        }

        public string DisplayName => PredefinedCommandHandlerNames.EditAndContinueFileSave;

        void IChainedCommandHandler<SaveCommandArgs>.ExecuteCommand(SaveCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            var textContainer = args.SubjectBuffer.AsTextContainer();

            if (Workspace.TryGetWorkspace(textContainer, out var workspace))
            {
                var encService = workspace.Services.GetService<IEditAndContinueWorkspaceService>();
                if (encService != null)
                {
                    var documentId = workspace.GetDocumentIdInCurrentContext(textContainer);
                    if (documentId != null)
                    {
                        encService.OnSourceFileUpdated(documentId);
                    }
                }
            }

            nextCommandHandler();
        }

        public VSCommanding.CommandState GetCommandState(SaveCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler)
            => nextCommandHandler();
    }
}


