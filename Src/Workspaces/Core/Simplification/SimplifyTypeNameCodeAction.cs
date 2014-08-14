using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal class SimplifyTypeNameCodeAction : CodeAction
    {
        private readonly string title;
        private readonly Func<CancellationToken, Task<Document>> createChangedDocument;

        public SimplifyTypeNameCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
        {
            this.title = title;
            this.createChangedDocument = createChangedDocument;
        }

        public override string Title
        {
            get { return this.title; }
        }

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            return this.createChangedDocument(cancellationToken);
        }
    }
}
