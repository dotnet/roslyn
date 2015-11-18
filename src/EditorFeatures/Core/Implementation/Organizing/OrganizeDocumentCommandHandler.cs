// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Organizing;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Organizing
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.OrganizeDocument,
        ContentTypeNames.CSharpContentType,
        ContentTypeNames.VisualBasicContentType)]
    internal class OrganizeDocumentCommandHandler :
        ICommandHandler<OrganizeDocumentCommandArgs>,
        ICommandHandler<SortImportsCommandArgs>,
        ICommandHandler<RemoveUnnecessaryImportsCommandArgs>,
        ICommandHandler<SortAndRemoveUnnecessaryImportsCommandArgs>
    {
        protected readonly IWaitIndicator WaitIndicator;

        [ImportingConstructor]
        public OrganizeDocumentCommandHandler(
            IWaitIndicator waitIndicator)
        {
            Contract.ThrowIfNull(waitIndicator);

            this.WaitIndicator = waitIndicator;
        }

        public CommandState GetCommandState(OrganizeDocumentCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args, nextHandler, _ => EditorFeaturesResources.OrganizeDocument, needsSemantics: true);
        }

        public void ExecuteCommand(OrganizeDocumentCommandArgs args, Action nextHandler)
        {
            this.WaitIndicator.Wait(
                title: EditorFeaturesResources.OrganizeDocument,
                message: EditorFeaturesResources.OrganizingDocument,
                allowCancel: true,
                action: waitContext => this.Organize(args.SubjectBuffer, waitContext.CancellationToken));
        }

        public CommandState GetCommandState(SortImportsCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args, nextHandler, o => o.SortImportsDisplayStringWithAccelerator, needsSemantics: false);
        }

        public void ExecuteCommand(SortImportsCommandArgs args, Action nextHandler)
        {
            this.WaitIndicator.Wait(
                title: EditorFeaturesResources.OrganizeDocument,
                message: EditorFeaturesResources.OrganizingDocument,
                allowCancel: true,
                action: waitContext => this.SortImports(args.SubjectBuffer, waitContext.CancellationToken));
        }

        public CommandState GetCommandState(RemoveUnnecessaryImportsCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args, nextHandler, o => o.RemoveUnusedImportsDisplayStringWithAccelerator, needsSemantics: true);
        }

        public void ExecuteCommand(RemoveUnnecessaryImportsCommandArgs args, Action nextHandler)
        {
            this.WaitIndicator.Wait(
                title: EditorFeaturesResources.OrganizeDocument,
                message: EditorFeaturesResources.OrganizingDocument,
                allowCancel: true,
                action: waitContext => this.RemoveUnusedImports(args.SubjectBuffer, waitContext.CancellationToken));
        }

        public CommandState GetCommandState(SortAndRemoveUnnecessaryImportsCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args, nextHandler, o => o.SortAndRemoveUnusedImportsDisplayStringWithAccelerator, needsSemantics: true);
        }

        private CommandState GetCommandState(CommandArgs args, Func<CommandState> nextHandler, Func<IOrganizeImportsService, string> descriptionString, bool needsSemantics)
        {
            Workspace workspace;
            if (IsCommandSupported(args, needsSemantics, out workspace))
            {
                var organizeImportsService = workspace.Services.GetLanguageServices(args.SubjectBuffer).GetService<IOrganizeImportsService>();
                return new CommandState(isAvailable: true, displayText: descriptionString(organizeImportsService));
            }
            else
            {
                return nextHandler();
            }
        }

        private bool IsCommandSupported(CommandArgs args, bool needsSemantics, out Workspace workspace)
        {
            workspace = null;
            var document = args.SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();

            if (document == null)
            {
                return false;
            }

            workspace = document.Project.Solution.Workspace;

            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return false;
            }

            if (workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return !needsSemantics;
            }

            return workspace.Services.GetService<IDocumentSupportsFeatureService>().SupportsRefactorings(document);
        }

        public void ExecuteCommand(SortAndRemoveUnnecessaryImportsCommandArgs args, Action nextHandler)
        {
            this.WaitIndicator.Wait(
                title: EditorFeaturesResources.OrganizeDocument,
                message: EditorFeaturesResources.OrganizingDocument,
                allowCancel: true,
                action: waitContext => this.SortAndRemoveUnusedImports(args.SubjectBuffer, waitContext.CancellationToken));
        }

        private void Organize(ITextBuffer subjectBuffer, CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var newDocument = OrganizingService.OrganizeAsync(document, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
                if (document != newDocument)
                {
                    ApplyTextChange(document, newDocument);
                }
            }
        }

        private void SortImports(ITextBuffer subjectBuffer, CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var newDocument = OrganizeImportsService.OrganizeImportsAsync(document, subjectBuffer.GetOption(OrganizerOptions.PlaceSystemNamespaceFirst), cancellationToken).WaitAndGetResult(cancellationToken);
                if (document != newDocument)
                {
                    ApplyTextChange(document, newDocument);
                }
            }
        }

        private void RemoveUnusedImports(ITextBuffer subjectBuffer, CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var newDocument = document.GetLanguageService<IRemoveUnnecessaryImportsService>().RemoveUnnecessaryImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                if (document != newDocument)
                {
                    ApplyTextChange(document, newDocument);
                }
            }
        }

        private void SortAndRemoveUnusedImports(ITextBuffer subjectBuffer, CancellationToken cancellationToken)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var newDocument = document.GetLanguageService<IRemoveUnnecessaryImportsService>().RemoveUnnecessaryImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                newDocument = OrganizeImportsService.OrganizeImportsAsync(newDocument, subjectBuffer.GetOption(OrganizerOptions.PlaceSystemNamespaceFirst), cancellationToken).WaitAndGetResult(cancellationToken);
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
