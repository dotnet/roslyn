// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Roslyn Completion Source Provider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class CompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IUIThreadOperationExecutor _operationExecutor;
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionSourceProvider(
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor operationExecutor,
            IAsynchronousOperationListenerProvider listenerProvider,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
            IGlobalOptionService globalOptions)
        {
            _threadingContext = threadingContext;
            _operationExecutor = operationExecutor;
            _streamingPresenter = streamingPresenter;
            _listener = listenerProvider.GetListener(FeatureAttribute.CompletionSet);
            _globalOptions = globalOptions;
        }

        public IAsyncCompletionSource? GetOrCreate(ITextView textView)
        {
            if (textView.TextBuffer.IsInLspEditorContext())
                return null;

            return new CompletionSource(textView, _streamingPresenter, _threadingContext, _operationExecutor, _listener, _globalOptions);
        }
    }
}
