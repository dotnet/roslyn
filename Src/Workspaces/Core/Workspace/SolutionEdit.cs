using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Services;

namespace Roslyn.Services
{
    public abstract partial class SolutionEdit : ISolutionEdit
    {
        private readonly ISolution baseSnapshot;
        private readonly string description;
        private readonly Dictionary<DocumentId, DocumentEdit> documentEdits;
        private bool active;

        public SolutionEdit(ISolution baseSnapshot, string description)
        {
            if (baseSnapshot == null)
            {
                throw new ArgumentNullException("baseSnapshot");
            }

            this.baseSnapshot = baseSnapshot;
            this.description = description;
            this.documentEdits = new Dictionary<DocumentId, DocumentEdit>();
            this.active = true;
        }

        internal void CheckActive()
        {
            if (!active)
            {
                // TODO: NeedsLocalization
                throw new InvalidOperationException("The edit is no longer valid.");
            }
        }

        public ISolution Solution
        {
            get { return baseSnapshot; }
        }

        public void Apply()
        {
            CheckActive();

            try
            {
                var consolidatedChanges = new Dictionary<DocumentId, IList<TextChange>>();

                foreach (var pair in documentEdits)
                {
                    consolidatedChanges.Add(pair.Key, pair.Value.GetChanges());
                }

                Apply(consolidatedChanges);
            }
            finally
            {
                // We invalidate this edit, whether we succeeded or failed
                active = false;
            }
        }

        public string Description { get { return description; } }

        protected abstract void Apply(IDictionary<DocumentId, IList<TextChange>> changes);

        public IDocumentEdit GetDocumentEdit(DocumentId documentId)
        {
            CheckActive();

            var document = baseSnapshot.GetDocument(documentId);
            if (document == null)
            {
                // TODO: NeedsLocalization
                throw new ArgumentException("The documentId is not a part of the workspace.", "documentId");
            }

            DocumentEdit edit;

            if (documentEdits.TryGetValue(documentId, out edit))
            {
                return edit;
            }

            edit = new DocumentEdit(document, this);
            documentEdits.Add(documentId, edit);

            return edit;
        }
    }
}
