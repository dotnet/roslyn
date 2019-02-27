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
            // TODO: Things go wrong if you change the name to an existing file name

            var newSolution = workspace.CurrentSolution.WithDocumentName(_oldDocumentId, _newFileName);
            return workspace.TryApplyChanges(newSolution, progressTracker);
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            TryApply(workspace, new ProgressTracker(), cancellationToken);
        }
    }
}
