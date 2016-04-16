// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TextDiffing
{
    [ExportWorkspaceService(typeof(IDocumentTextDifferencingService), ServiceLayer.Host), Shared]
    internal class EditorTextDifferencingService : IDocumentTextDifferencingService
    {
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly ITextDifferencingSelectorService _differenceSelectorService;

        [ImportingConstructor]
        public EditorTextDifferencingService(ITextBufferFactoryService textBufferFactoryService, ITextDifferencingSelectorService differenceSelectorService)
        {
            _textBufferFactoryService = textBufferFactoryService;
            _differenceSelectorService = differenceSelectorService;
        }

        public async Task<IEnumerable<TextChange>> GetTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
        {
            var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var diffService = _differenceSelectorService.GetTextDifferencingService(oldDocument.Project.LanguageServices.GetService<IContentTypeLanguageService>().GetDefaultContentType())
                ?? _differenceSelectorService.DefaultTextDifferencingService;

            var differenceOptions = new StringDifferenceOptions()
            {
                DifferenceType = StringDifferenceTypes.Word
            };

            var oldTextSnapshot = oldText.FindCorrespondingEditorTextSnapshot();
            var newTextSnapshot = newText.FindCorrespondingEditorTextSnapshot();
            var useSnapshots = oldTextSnapshot != null && newTextSnapshot != null;

            var diffResult = useSnapshots
                ? diffService.DiffSnapshotSpans(oldTextSnapshot.GetFullSpan(), newTextSnapshot.GetFullSpan(), differenceOptions)
                : diffService.DiffStrings(oldText.ToString(), newText.ToString(), differenceOptions);

            return diffResult.Differences.Select(d =>
                new TextChange(
                    diffResult.LeftDecomposition.GetSpanInOriginal(d.Left).ToTextSpan(),
                    newText.GetSubText(diffResult.RightDecomposition.GetSpanInOriginal(d.Right).ToTextSpan()).ToString()));
        }
    }
}
