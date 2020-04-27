﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface
{
    internal abstract class AbstractExtractInterfaceCommandHandler : ICommandHandler<ExtractInterfaceCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;

        protected AbstractExtractInterfaceCommandHandler(IThreadingContext threadingContext)
            => this._threadingContext = threadingContext;

        public string DisplayName => EditorFeaturesResources.Extract_Interface;

        public CommandState GetCommandState(ExtractInterfaceCommandArgs args)
            => IsAvailable(args.SubjectBuffer, out _) ? CommandState.Available : CommandState.Unspecified;

        public bool ExecuteCommand(ExtractInterfaceCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Extract_Interface))
            {
                var subjectBuffer = args.SubjectBuffer;
                if (!IsAvailable(subjectBuffer, out var workspace))
                {
                    return false;
                }

                var caretPoint = args.TextView.GetCaretPoint(subjectBuffer);
                if (!caretPoint.HasValue)
                {
                    return false;
                }

                var document = subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                    context.OperationContext, _threadingContext);
                if (document == null)
                {
                    return false;
                }

                // We are about to show a modal UI dialog so we should take over the command execution
                // wait context. That means the command system won't attempt to show its own wait dialog 
                // and also will take it into consideration when measuring command handling duration.
                context.OperationContext.TakeOwnership();

                var moveMembersService = document.GetRequiredLanguageService<AbstractMoveMembersService>();

                var moveMembersOptions = _threadingContext.JoinableTaskFactory.Run(async () =>
                {
                    var analysis = await moveMembersService.AnalyzeAsync(document, new TextSpan(caretPoint.Value.Position, 0), context.OperationContext.UserCancellationToken).ConfigureAwait(false);
                    var moveMembersOptionService = document.Project.Solution.Workspace.Services.GetRequiredService<IMoveMembersOptionService>();

                    return moveMembersOptionService.GetMoveMembersOptions(document, analysis, MoveMembersEntryPoint.ExtractInterface);
                });

                if (moveMembersOptions?.Destination is null)
                {
                    return true;
                }

                var result = moveMembersService.MoveMembersAsync(document, moveMembersOptions, context.OperationContext.UserCancellationToken).WaitAndGetResult_CanCallOnBackground(context.OperationContext.UserCancellationToken);

                if (result.Success == false || !document.Project.Solution.Workspace.TryApplyChanges(result.Solution))
                {
                    // TODO: handle failure
                    return true;
                }

                var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
                navigationService.TryNavigateToPosition(workspace, result.NavigationDocumentId, 0);

                return true;
            }
        }

        private static bool IsAvailable(ITextBuffer subjectBuffer, out Workspace workspace)
            => subjectBuffer.TryGetWorkspace(out workspace) &&
               workspace.CanApplyChange(ApplyChangesKind.AddDocument) &&
               workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) &&
               subjectBuffer.SupportsRefactorings();
    }
}
