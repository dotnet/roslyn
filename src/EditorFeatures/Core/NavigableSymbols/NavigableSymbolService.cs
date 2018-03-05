// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols
{
    [Export(typeof(INavigableSymbolSourceProvider))]
    [Name(nameof(NavigableSymbolService))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class NavigableSymbolService : INavigableSymbolSourceProvider
    {
        private static readonly object s_key = new object();
        private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _streamingPresenters;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public NavigableSymbolService(
            IWaitIndicator waitIndicator,
            [ImportMany] IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters)
        {
            _waitIndicator = waitIndicator;
            _streamingPresenters = streamingPresenters;
        }

        public INavigableSymbolSource TryCreateNavigableSymbolSource(ITextView textView, ITextBuffer buffer)
        {
            return textView.GetOrCreatePerSubjectBufferProperty(buffer, s_key,
                (v, b) => new NavigableSymbolSource(_streamingPresenters, _waitIndicator));
        }
    }
}
