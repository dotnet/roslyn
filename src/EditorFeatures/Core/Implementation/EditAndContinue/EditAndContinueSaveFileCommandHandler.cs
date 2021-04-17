// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

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
                var documentId = workspace.GetDocumentIdInCurrentContext(textContainer);
                if (documentId != null)
                {
                    // ignoring source-generated files since they shouldn't be modified and saved:
                    var currentDocument = workspace.CurrentSolution.GetDocument(documentId);
                    if (currentDocument != null)
                    {
                        var proxy = new RemoteEditAndContinueServiceProxy(workspace);

                        // fire and forget
                        _ = Task.Run(() => proxy.OnSourceFileUpdatedAsync(currentDocument, CancellationToken.None)).ReportNonFatalErrorAsync();
                    }
                }
            }

            nextCommandHandler();
        }

        public VSCommanding.CommandState GetCommandState(SaveCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler)
            => nextCommandHandler();
    }
}

