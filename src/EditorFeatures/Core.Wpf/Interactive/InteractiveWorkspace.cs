// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    internal sealed class InteractiveWorkspace : Workspace
    {
        public readonly InteractiveEvaluator Evaluator;
        private readonly ISolutionCrawlerRegistrationService _registrationService;

        internal IInteractiveWindow Window { get; set; }
        private SourceTextContainer _openTextContainer;
        private DocumentId _openDocumentId;

        internal InteractiveWorkspace(HostServices hostServices, InteractiveEvaluator evaluator)
            : base(hostServices, WorkspaceKind.Interactive)
        {
            // register work coordinator for this workspace
            _registrationService = Services.GetService<ISolutionCrawlerRegistrationService>();
            _registrationService.Register(this);

            Evaluator = evaluator;
        }

        protected override void Dispose(bool finalize)
        {
            // workspace is going away. unregister this workspace from work coordinator
            _registrationService.Unregister(this, blockingShutdown: true);

            base.Dispose(finalize);
        }

        public new void SetCurrentSolution(Solution solution)
        {
            var oldSolution = this.CurrentSolution;
            var newSolution = base.SetCurrentSolution(solution);
            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
        }

        public override bool CanOpenDocuments
        {
            get { return true; }
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.ChangeDocument:
                    return true;

                default:
                    return false;
            }
        }

        public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
        {
            _openTextContainer = textContainer;
            _openDocumentId = documentId;
            this.OnDocumentOpened(documentId, textContainer);
        }

        protected override void ApplyDocumentTextChanged(DocumentId document, SourceText newText)
        {
            if (_openDocumentId != document)
            {
                return;
            }

            ITextSnapshot appliedText;
            using (var edit = _openTextContainer.GetTextBuffer().CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null))
            {
                var oldText = _openTextContainer.CurrentText;
                var changes = newText.GetTextChanges(oldText);

                foreach (var change in changes)
                {
                    edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
                }

                appliedText = edit.Apply();
            }

            this.OnDocumentTextChanged(document, appliedText.AsText(), PreservationMode.PreserveIdentity);
        }

        public new void ClearSolution()
        {
            base.ClearSolution();
        }

        internal new void ClearOpenDocument(DocumentId documentId)
        {
            base.ClearOpenDocument(documentId);
        }

        internal new void UnregisterText(SourceTextContainer textContainer)
        {
            base.UnregisterText(textContainer);
        }
    }
}
