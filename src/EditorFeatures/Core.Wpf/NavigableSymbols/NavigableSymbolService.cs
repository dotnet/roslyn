// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.NavigableSymbols
{
    [Export(typeof(INavigableSymbolSourceProvider))]
    [Name(nameof(NavigableSymbolService))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal sealed partial class NavigableSymbolService : INavigableSymbolSourceProvider
    {
        private static readonly object s_key = new();

        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public NavigableSymbolService(
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
            _threadingContext = threadingContext;
            _listener = listenerProvider.GetListener(FeatureAttribute.NavigableSymbols);
        }

        public INavigableSymbolSource TryCreateNavigableSymbolSource(ITextView textView, ITextBuffer buffer)
            => textView.GetOrCreatePerSubjectBufferProperty(buffer, s_key, (view, _) => new NavigableSymbolSource(this, view));
    }
}
