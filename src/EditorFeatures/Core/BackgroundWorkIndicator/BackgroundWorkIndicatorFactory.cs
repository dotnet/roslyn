// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
{
    [Export(typeof(IBackgroundWorkIndicatorFactory)), Shared]
    internal partial class BackgroundWorkIndicatorFactory : IBackgroundWorkIndicatorFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IToolTipPresenterFactory _toolTipPresenterFactory;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BackgroundWorkIndicatorFactory(
            IThreadingContext threadingContext,
            IToolTipPresenterFactory toolTipPresenterFactory,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _toolTipPresenterFactory = toolTipPresenterFactory;
            _listener = listenerProvider.GetListener(FeatureAttribute.QuickInfo);
        }

        IUIThreadOperationContext IBackgroundWorkIndicatorFactory.Create(
            ITextView textView,
            SnapshotSpan applicableToSpan,
            string description,
            bool cancelOnEdit,
            bool cancelOnFocusLost)
        {
            // Create the indicator in its default/empty state.
            var context = (IUIThreadOperationContext)new BackgroundWorkIndicatorContext(
                this, textView, applicableToSpan, description,
                cancelOnEdit, cancelOnFocusLost);

            // Then add a single scope representing the how the UI should look initially.
            context.AddScope(allowCancellation: true, description);
            return context;
        }
    }
}
