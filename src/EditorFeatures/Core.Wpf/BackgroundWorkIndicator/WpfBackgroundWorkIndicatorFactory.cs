// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
{
    [Export(typeof(WpfBackgroundWorkIndicatorFactory))]
    [ExportWorkspaceService(typeof(IBackgroundWorkIndicatorFactory), ServiceLayer.Editor), Shared]
    internal sealed partial class WpfBackgroundWorkIndicatorFactory : IBackgroundWorkIndicatorFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IToolTipPresenterFactory _toolTipPresenterFactory;
        private readonly IAsynchronousOperationListener _listener;

        private BackgroundWorkIndicatorContext? _currentContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WpfBackgroundWorkIndicatorFactory(
            IThreadingContext threadingContext,
            IToolTipPresenterFactory toolTipPresenterFactory,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _toolTipPresenterFactory = toolTipPresenterFactory;
            _listener = listenerProvider.GetListener(FeatureAttribute.QuickInfo);
        }

        IBackgroundWorkIndicatorContext IBackgroundWorkIndicatorFactory.Create(
            ITextView textView,
            SnapshotSpan applicableToSpan,
            string description,
            bool cancelOnEdit,
            bool cancelOnFocusLost)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // If we have an outstanding context in flight, cancel it and create a new one to show the user.
            _currentContext?.CancelAndDispose();

            // Create the indicator in its default/empty state.
            _currentContext = new BackgroundWorkIndicatorContext(
                this, textView, applicableToSpan, description,
                cancelOnEdit, cancelOnFocusLost);

            // Then add a single scope representing the how the UI should look initially.
            _currentContext.AddScope(allowCancellation: true, description);
            return _currentContext;
        }

        private void OnContextDisposed(BackgroundWorkIndicatorContext context)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (_currentContext == context)
                _currentContext = null;
        }
    }
}
