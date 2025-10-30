// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation;

internal abstract partial class AbstractTextStructureNavigatorProvider(
    ITextStructureNavigatorSelectorService selectorService,
    IContentTypeRegistryService contentTypeService,
    IUIThreadOperationExecutor uIThreadOperationExecutor) : ITextStructureNavigatorProvider
{
    private readonly ITextStructureNavigatorSelectorService _selectorService = selectorService;
    private readonly IContentTypeRegistryService _contentTypeService = contentTypeService;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor = uIThreadOperationExecutor;

    protected abstract bool ShouldSelectEntireTriviaFromStart(SyntaxTrivia trivia);

    protected abstract TextExtent GetExtentOfWordFromToken(ITextStructureNavigator naturalLanguageNavigator, SyntaxToken token, SnapshotPoint position);

    protected static TextExtent GetTokenExtent(SyntaxToken token, ITextSnapshot snapshot)
        => new(token.Span.ToSnapshotSpan(snapshot), isSignificant: true);

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
