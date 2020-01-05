// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Peek;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    [Export(typeof(IPeekableItemSourceProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name("Roslyn Peekable Item Provider")]
    [SupportsStandaloneFiles(true)]
    [SupportsPeekRelationship("IsDefinedBy")]
    internal sealed class PeekableItemSourceProvider : IPeekableItemSourceProvider
    {
        private readonly IPeekableItemFactory _peekableItemFactory;
        private readonly IPeekResultFactory _peekResultFactory;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public PeekableItemSourceProvider(
            IPeekableItemFactory peekableItemFactory,
            IPeekResultFactory peekResultFactory,
            IWaitIndicator waitIndicator)
        {
            _peekableItemFactory = peekableItemFactory;
            _peekResultFactory = peekResultFactory;
            _waitIndicator = waitIndicator;
        }

        public IPeekableItemSource TryCreatePeekableItemSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new PeekableItemSource(textBuffer, _peekableItemFactory, _peekResultFactory, _waitIndicator));
        }
    }
}
