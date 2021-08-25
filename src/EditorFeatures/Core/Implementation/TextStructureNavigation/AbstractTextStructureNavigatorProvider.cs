// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation
{
    internal abstract partial class AbstractTextStructureNavigatorProvider : ITextStructureNavigatorProvider
    {
        private readonly ITextStructureNavigatorSelectorService _selectorService;
        private readonly IContentTypeRegistryService _contentTypeService;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

        protected AbstractTextStructureNavigatorProvider(
            ITextStructureNavigatorSelectorService selectorService,
            IContentTypeRegistryService contentTypeService,
            IUIThreadOperationExecutor uIThreadOperationExecutor)
        {
            Contract.ThrowIfNull(selectorService);
            Contract.ThrowIfNull(contentTypeService);

            _selectorService = selectorService;
            _contentTypeService = contentTypeService;
            _uiThreadOperationExecutor = uIThreadOperationExecutor;
        }

        protected abstract bool ShouldSelectEntireTriviaFromStart(SyntaxTrivia trivia);
        protected abstract bool IsWithinNaturalLanguage(SyntaxToken token, int position);

        protected virtual TextExtent GetExtentOfWordFromToken(SyntaxToken token, SnapshotPoint position)
            => new(token.Span.ToSnapshotSpan(position.Snapshot), isSignificant: true);

        public ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer subjectBuffer)
        {
            var naturalLanguageNavigator = _selectorService.CreateTextStructureNavigator(
                subjectBuffer,
                _contentTypeService.GetContentType("any"));

            return new TextStructureNavigator(
                subjectBuffer,
                naturalLanguageNavigator,
                this,
                _uiThreadOperationExecutor);
        }
    }
}
