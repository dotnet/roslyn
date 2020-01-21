// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Organizing;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Organizing
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [Name(PredefinedCommandHandlerNames.OrganizeDocument)]
    internal class OrganizeDocumentCommandHandler :
        ICommandHandler<OrganizeDocumentCommandArgs>,
        ICommandHandler<SortImportsCommandArgs>,
        ICommandHandler<SortAndRemoveUnnecessaryImportsCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public OrganizeDocumentCommandHandler(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public string DisplayName => EditorFeaturesResources.Organize_Document;

        public CommandState GetCommandState(OrganizeDocumentCommandArgs args)
        {
            return GetCommandState(args, _ => EditorFeaturesResources.Organize_Document, needsSemantics: true);
        }

        public bool ExecuteCommand(OrganizeDocumentCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Organizing_document))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;
                var document = args.SubjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                    context.OperationContext, _threadingContext);
                if (document != null)
                {
                    var newDocument = OrganizingService.OrganizeAsync(document, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
                    if (document != newDocument)
                    {
                        ApplyTextChange(document, newDocument);
                    }
                }
            }

            return true;
        }

        public CommandState GetCommandState(SortImportsCommandArgs args)
        {
            return GetCommandState(args, o => o.SortImportsDisplayStringWithAccelerator, needsSemantics: false);
        }

        public CommandState GetCommandState(SortAndRemoveUnnecessaryImportsCommandArgs args)
        {
            return GetCommandState(args, o => o.SortAndRemoveUnusedImportsDisplayStringWithAccelerator, needsSemantics: true);
        }

        private CommandState GetCommandState(EditorCommandArgs args, Func<IOrganizeImportsService, string> descriptionString, bool needsSemantics)
        {
            if (IsCommandSupported(args, needsSemantics, out var workspace))
            {
                var organizeImportsService = workspace.Services.GetLanguageServices(args.SubjectBuffer).GetService<IOrganizeImportsService>();
                return new CommandState(isAvailable: true, displayText: descriptionString(organizeImportsService));
            }
            else
            {
                return CommandState.Unspecified;
            }
        }

        private bool IsCommandSupported(EditorCommandArgs args, bool needsSemantics, out Workspace workspace)
        {
            workspace = null;
            if (args.SubjectBuffer.TryGetWorkspace(out var retrievedWorkspace))
            {
                workspace = retrievedWorkspace;
                if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
                {
                    return false;
                }

                if (workspace.Kind == WorkspaceKind.MiscellaneousFiles)
                {
                    return !needsSemantics;
                }

                return args.SubjectBuffer.SupportsRefactorings();
            }

            return false;
        }

        public bool ExecuteCommand(SortImportsCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Organizing_document))
            {
                this.SortImports(args.SubjectBuffer, context.OperationContext);
            }

            return true;
        }

        public bool ExecuteCommand(SortAndRemoveUnnecessaryImportsCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Organizing_document))
            {
                this.SortAndRemoveUnusedImports(args.SubjectBuffer, context.OperationContext);
            }

            return true;
        }

        private void SortImports(ITextBuffer subjectBuffer, IUIThreadOperationContext operationContext)
        {
            var cancellationToken = operationContext.UserCancellationToken;
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var newDocument = Formatter.OrganizeImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                if (document != newDocument)
                {
                    ApplyTextChange(document, newDocument);
                }
            }
        }

        private void SortAndRemoveUnusedImports(ITextBuffer subjectBuffer, IUIThreadOperationContext operationContext)
        {
            var cancellationToken = operationContext.UserCancellationToken;
            var document = subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                operationContext, _threadingContext);
            if (document != null)
            {
                var newDocument = document.GetLanguageService<IRemoveUnnecessaryImportsService>().RemoveUnnecessaryImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                newDocument = Formatter.OrganizeImportsAsync(newDocument, cancellationToken).WaitAndGetResult(cancellationToken);
                if (document != newDocument)
                {
                    ApplyTextChange(document, newDocument);
                }
            }
        }

        protected static void ApplyTextChange(Document oldDocument, Document newDocument)
        {
            oldDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, CancellationToken.None);
        }
    }
}
