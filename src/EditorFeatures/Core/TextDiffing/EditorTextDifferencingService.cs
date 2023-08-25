// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
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
        private readonly ITextDifferencingSelectorService _differenceSelectorService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorTextDifferencingService(ITextDifferencingSelectorService differenceSelectorService)
            => _differenceSelectorService = differenceSelectorService;

        public Task<ImmutableArray<TextChange>> GetTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
            => GetTextChangesAsync(oldDocument, newDocument, TextDifferenceTypes.Word, cancellationToken);

        public async Task<ImmutableArray<TextChange>> GetTextChangesAsync(Document oldDocument, Document newDocument, TextDifferenceTypes preferredDifferenceType, CancellationToken cancellationToken)
        {
            var oldText = await oldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var diffService = _differenceSelectorService.GetTextDifferencingService(oldDocument.Project.Services.GetService<IContentTypeLanguageService>().GetDefaultContentType())
                ?? _differenceSelectorService.DefaultTextDifferencingService;

            var differenceOptions = GetDifferenceOptions(preferredDifferenceType);

            var oldTextSnapshot = oldText.FindCorrespondingEditorTextSnapshot();
            var newTextSnapshot = newText.FindCorrespondingEditorTextSnapshot();
            var useSnapshots = oldTextSnapshot != null && newTextSnapshot != null;

            var diffResult = useSnapshots
                ? diffService.DiffSnapshotSpans(oldTextSnapshot.GetFullSpan(), newTextSnapshot.GetFullSpan(), differenceOptions)
                : diffService.DiffStrings(oldText.ToString(), newText.ToString(), differenceOptions);

            return diffResult.Differences.Select(d =>
                new TextChange(
                    diffResult.LeftDecomposition.GetSpanInOriginal(d.Left).ToTextSpan(),
                    newText.GetSubText(diffResult.RightDecomposition.GetSpanInOriginal(d.Right).ToTextSpan()).ToString())).ToImmutableArray();
        }

        private static StringDifferenceOptions GetDifferenceOptions(TextDifferenceTypes differenceTypes)
        {
            StringDifferenceTypes stringDifferenceTypes = default;

            if (differenceTypes.HasFlag(TextDifferenceTypes.Line))
            {
                stringDifferenceTypes |= StringDifferenceTypes.Line;
            }

            if (differenceTypes.HasFlag(TextDifferenceTypes.Word))
            {
                stringDifferenceTypes |= StringDifferenceTypes.Word;
            }

            if (differenceTypes.HasFlag(TextDifferenceTypes.Character))
            {
                stringDifferenceTypes |= StringDifferenceTypes.Character;
            }

            return new StringDifferenceOptions()
            {
                DifferenceType = stringDifferenceTypes
            };
        }
    }
}
