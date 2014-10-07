using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Compilers;

namespace Roslyn.Services
{
    public abstract partial class SolutionEdit
    {
        private class DocumentEdit : IDocumentEdit
        {
            private readonly List<TextChange> changes;
            private readonly IDocument document;
            private readonly int documentLength;
            private readonly SolutionEdit parentEdit;

            public DocumentEdit(IDocument document, SolutionEdit parentEdit)
            {
                this.changes = new List<TextChange>();
                this.document = document;

                // TODO: should we expose an IText directly on document snapshot?
                this.documentLength = document.GetSyntaxTree().GetText().Length;
                this.parentEdit = parentEdit;
            }

            public IDocument Document
            {
                get { return document; }
            }

            [ExcludeFromCodeCoverage]
            [Obsolete("Never called presently. Please add coverage if you start using it.")]
            public void Delete(TextSpan deleteSpan)
            {
                if (deleteSpan.End > documentLength)
                {
                    // TODO: NeedsLocalization
                    throw new ArgumentException("The deleteSpan exceeds the length of the document.", "deleteSpan");
                }

                parentEdit.CheckActive();
                changes.Add(new TextChange(deleteSpan, string.Empty));
            }

            [ExcludeFromCodeCoverage]
            [Obsolete("Never called presently. Please add coverage if you start using it.")]
            public void Insert(int position, string text)
            {
                if (position > documentLength)
                {
                    // TODO: NeedsLocalization
                    throw new ArgumentException("The deleteSpan exceeds the length of the document.", "deleteSpan");
                }

                parentEdit.CheckActive();
                changes.Add(new TextChange(new TextSpan(position, 0), text));
            }

            public void Replace(TextSpan replaceSpan, string replaceWith)
            {
                if (replaceSpan.End > documentLength)
                {
                    // TODO: NeedsLocalization
                    throw new ArgumentException("The replaceSpan exceeds the length of the document.", "replaceSpan");
                }

                parentEdit.CheckActive();
                changes.Add(new TextChange(replaceSpan, replaceWith));
            }

            internal IList<TextChange> GetChanges()
            {
                return new List<TextChange>(changes).AsReadOnly();
            }
        }
    }
}
