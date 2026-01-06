// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek;

[Export(typeof(IPeekableItemSourceProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
[Name("Roslyn Peekable Item Provider")]
[SupportsStandaloneFiles(true)]
[SupportsPeekRelationship("IsDefinedBy")]
internal sealed class PeekableItemSourceProvider : IPeekableItemSourceProvider
{
    private readonly PeekableItemFactory _peekableItemFactory;
    private readonly IPeekResultFactory _peekResultFactory;
    private readonly IThreadingContext _threadingContext;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PeekableItemSourceProvider(
        PeekableItemFactory peekableItemFactory,
        IPeekResultFactory peekResultFactory,
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor uiThreadOperationExecutor)
    {
        _peekableItemFactory = peekableItemFactory;
        _peekResultFactory = peekResultFactory;
        _threadingContext = threadingContext;
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
    }

    public IPeekableItemSource? TryCreatePeekableItemSource(ITextBuffer textBuffer)
        => textBuffer.Properties.GetOrCreateSingletonProperty(() =>
            new PeekableItemSource(textBuffer, _peekableItemFactory, _peekResultFactory, _threadingContext, _uiThreadOperationExecutor));
}
