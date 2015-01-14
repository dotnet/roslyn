// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editting
{
    /// <summary>
    /// An editor for making changes to multiple documents in a solution.
    /// </summary>
    public class SolutionEditor
    {
        private readonly Solution solution;
        private readonly Dictionary<DocumentId, DocumentEditor> documentEditors;

        public SolutionEditor(Solution solution)
        {
            this.solution = solution;
            this.documentEditors = new Dictionary<DocumentId, DocumentEditor>();
        }

        /// <summary>
        /// The <see cref="Solution"/> that was specified when the <see cref="SolutionEditor"/> was constructed.
        /// </summary>
        public Solution OriginalSolution
        {
            get { return this.solution; }
        }

        /// <summary>
        /// Gets the <see cref="DocumentEditor"/> for the corresponding <see cref="DocumentId"/>.
        /// </summary>
        public async Task<DocumentEditor> GetDocumentEditorAsync(DocumentId id, CancellationToken cancellationToken = default(CancellationToken))
        {
            DocumentEditor editor;
            if (!this.documentEditors.TryGetValue(id, out editor))
            {
                editor = await DocumentEditor.CreateAsync(this.solution.GetDocument(id), cancellationToken).ConfigureAwait(false);
                this.documentEditors.Add(id, editor);
            }

            return editor;
        }

        /// <summary>
        /// Returns the changed <see cref="Solution"/>.
        /// </summary>
        public Solution GetChangedSolution()
        {
            var changedSolution = this.solution;

            foreach (var docEd in this.documentEditors.Values)
            {
                var currentDoc = changedSolution.GetDocument(docEd.OriginalDocument.Id);
                var newDoc = currentDoc.WithSyntaxRoot(docEd.GetChangedRoot());
                changedSolution = newDoc.Project.Solution;
            }

            return changedSolution;
        }
    }
}
