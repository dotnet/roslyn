using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal class SimplifyTypeNameFixAllCodeAction : CodeAction
    {
        private readonly string title;
        private readonly Func<CancellationToken, Task<Document>> createChangedDocument;

        public SimplifyTypeNameFixAllCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
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

        protected override async Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
        {
            var optionSet = document.Project.Solution.Workspace.Options.WithChangedOption(FormattingOptions.AllowDisjointSpanMerging, true);
            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            document = await Formatter.FormatAsync(document, Formatter.Annotation, optionSet, cancellationToken: cancellationToken).ConfigureAwait(false);
            document = await CaseCorrector.CaseCorrectAsync(document, CaseCorrector.Annotation, cancellationToken).ConfigureAwait(false);
            return document;
        }
    }
}
