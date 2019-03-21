// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal class RenameDocumentOperation : CodeActionOperation
    {
        private readonly DocumentId _oldDocumentId;
        private readonly DocumentId _newDocumentId;
        private readonly string _newFileName;
        private readonly SourceText _text;

        public override string Title { get; }

        internal override bool ApplyDuringTests => true;

        public RenameDocumentOperation(
            DocumentId oldDocumentId,
            DocumentId newDocumentId,
            string newFileName,
            SourceText text)
        {
            _oldDocumentId = oldDocumentId;
            _newDocumentId = newDocumentId;
            _newFileName = newFileName;
            _text = text;
        }

        internal override bool TryApply(Workspace workspace, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var solution = workspace.CurrentSolution;

            // currently, document rename is accomplished by a remove followed by an add.
            // the workspace takes care of resolving conflicts if the document name is not unique in the project
            // by adding numeric suffixes to the new document being added.
            var oldDocument = solution.GetDocument(_oldDocumentId);
            var newSolution = solution.RemoveDocument(_oldDocumentId);

            newSolution = newSolution.AddDocument(_newDocumentId, _newFileName, _text, oldDocument.Folders);
            return workspace.TryApplyChanges(newSolution, progressTracker);
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            TryApply(workspace, new ProgressTracker(), cancellationToken);
        }
    }
}
