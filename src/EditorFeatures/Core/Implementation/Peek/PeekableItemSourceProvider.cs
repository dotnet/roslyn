﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
