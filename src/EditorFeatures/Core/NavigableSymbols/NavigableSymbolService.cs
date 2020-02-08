﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public NavigableSymbolService(
            IWaitIndicator waitIndicator,
            IStreamingFindUsagesPresenter streamingPresenter)
        {
            _waitIndicator = waitIndicator;
            _streamingPresenter = streamingPresenter;
        }

        public INavigableSymbolSource TryCreateNavigableSymbolSource(ITextView textView, ITextBuffer buffer)
        {
            return textView.GetOrCreatePerSubjectBufferProperty(buffer, s_key,
                (v, b) => new NavigableSymbolSource(_streamingPresenter, _waitIndicator));
        }
    }
}
