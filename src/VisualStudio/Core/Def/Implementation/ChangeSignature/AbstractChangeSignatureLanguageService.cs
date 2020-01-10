using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal abstract class AbstractChangeSignatureLanguageService : IChangeSignatureLanguageService
    {
        public abstract Task<IntellisenseTextBoxViewModel[]> CreateViewModelsAsync(
            string[] rolesCollectionType,
            string[] rolesCollectionName,
            int insertPosition,
            Document document,
            string documentText,
            IContentType contentType,
            IntellisenseTextBoxViewModelFactory intellisenseTextBoxViewModelFactory,
            CancellationToken cancellationToken);
        public abstract void GeneratePreviewGrammar(ChangeSignatureDialogViewModel.AddedParameterViewModel addedParameterViewModel, List<SymbolDisplayPart> displayParts);
        public abstract IContentType GetContentType(IContentTypeRegistryService contentTypeRegistryService);

        protected ITrackingSpan[] CreateTrackingSpansHelper(IProjectionSnapshot snapshot, int contextPoint, int spaceBetweenTypeAndName)
        {
            // Get the previous span/text.
            var previousStatementSpan = snapshot.CreateTrackingSpanFromStartToIndex(contextPoint, SpanTrackingMode.EdgeNegative);

            // Get the appropriate ITrackingSpan for the window the user is typing in.
            // mappedSpan1 is the 'Type' field for C# and 'Name' for VB.
            var mappedSpan1 = snapshot.CreateTrackingSpan(contextPoint, 0, SpanTrackingMode.EdgeExclusive);

            // Space could be either ' ' for C# or '] AS ' for VB
            var spaceSpan = snapshot.CreateTrackingSpan(contextPoint, spaceBetweenTypeAndName, SpanTrackingMode.EdgeExclusive);

            // mappedSpan2 is the 'Name' field for C# and 'Type' for VB.
            var mappedSpan2 = snapshot.CreateTrackingSpan(contextPoint + spaceBetweenTypeAndName, 0, SpanTrackingMode.EdgeExclusive);

            // Build the tracking span that includes the rest of the file.
            var restOfFileSpan = snapshot.CreateTrackingSpanFromIndexToEnd(contextPoint + spaceBetweenTypeAndName, SpanTrackingMode.EdgePositive);

            // This array as a whole should encompass the span of the entire file.
            return new ITrackingSpan[] { previousStatementSpan, mappedSpan1, spaceSpan, mappedSpan2, restOfFileSpan };
        }
    }
}
