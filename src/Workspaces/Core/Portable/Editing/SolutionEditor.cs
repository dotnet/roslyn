﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editing
{
    /// <summary>
    /// An editor for making changes to multiple documents in a solution.
    /// </summary>
    public class SolutionEditor
    {
        private readonly Solution _solution;
        private readonly Dictionary<DocumentId, DocumentEditor> _documentEditors;

        public SolutionEditor(Solution solution)
        {
            _solution = solution;
            _documentEditors = new Dictionary<DocumentId, DocumentEditor>();
        }

        /// <summary>
        /// The <see cref="Solution"/> that was specified when the <see cref="SolutionEditor"/> was constructed.
        /// </summary>
        public Solution OriginalSolution => _solution;

        /// <summary>
        /// Gets the <see cref="DocumentEditor"/> for the corresponding <see cref="DocumentId"/>.
        /// </summary>
        public async Task<DocumentEditor> GetDocumentEditorAsync(DocumentId id, CancellationToken cancellationToken = default)
        {
            if (!_documentEditors.TryGetValue(id, out var editor))
            {
                editor = await DocumentEditor.CreateAsync(_solution.GetDocument(id), cancellationToken).ConfigureAwait(false);
                _documentEditors.Add(id, editor);
            }

            return editor;
        }

        /// <summary>
        /// Returns the changed <see cref="Solution"/>.
        /// </summary>
        public Solution GetChangedSolution()
        {
            var changedSolution = _solution;

            foreach (var docEd in _documentEditors.Values)
            {
                var currentDoc = changedSolution.GetDocument(docEd.OriginalDocument.Id);
                var newDoc = currentDoc.WithSyntaxRoot(docEd.GetChangedRoot());
                changedSolution = newDoc.Project.Solution;
            }

            return changedSolution;
        }
    }
}
