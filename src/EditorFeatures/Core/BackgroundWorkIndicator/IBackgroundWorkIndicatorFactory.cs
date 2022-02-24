// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
{
    internal interface IBackgroundWorkIndicatorFactory
    {
        IUIThreadOperationContext Create(
            ITextView textView, SnapshotSpan applicableToSpan,
            string description, bool cancelOnEdit = true, bool cancelWhenOffScreen = true);
    }

    [Export(typeof(IBackgroundWorkIndicatorFactory)), Shared]
    internal class DefaultBackgroundWorkIndicatorFactory : IBackgroundWorkIndicatorFactory
    {
        private readonly IToolTipPresenterFactory _toolTipPresenterFactory;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultBackgroundWorkIndicatorFactory(
            IToolTipPresenterFactory toolTipPresenterFactory)
        {
            _toolTipPresenterFactory = toolTipPresenterFactory;
        }

        public IUIThreadOperationContext Create(
            ITextView textView,
            SnapshotSpan applicableToSpan,
            string description,
            bool cancelOnEdit = true,
            bool cancelWhenOffScreen = true)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var toolTipPresenter = _toolTipPresenterFactory.Create(textView, new ToolTipParameters(
                trackMouse: false,
                ignoreBufferChange: true,
                keepOpenFunc: null,
                ignoreCaretPositionChange: true,
                dismissWhenOffscreen: false));
            toolTipPresenter.StartOrUpdate(applicableToSpan.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive));
            var indicator = (IUIThreadOperationContext)new BackgroundWorkIndicator(
                toolTipPresenter, cancellationTokenSource);

            indicator.AddScope(allowCancellation: true, description);
            return indicator;
        }

        private class BackgroundWorkIndicator : IUIThreadOperationContext
        {
            private readonly IToolTipPresenter _toolTipPresenter;
            private readonly CancellationTokenSource _cancellationTokenSource;

            public BackgroundWorkIndicator(IToolTipPresenter toolTipPresenter, CancellationTokenSource cancellationTokenSource)
            {
                _toolTipPresenter = toolTipPresenter;
                _cancellationTokenSource = cancellationTokenSource;
            }

            public void Dispose()
            {
                _toolTipPresenter.Dismiss();
            }

            public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
            {
                throw new NotImplementedException();
            }

            public void TakeOwnership()
            {
                this.Dispose();
            }

            public CancellationToken UserCancellationToken => throw new NotImplementedException();

            public bool AllowCancellation => throw new NotImplementedException();

            public string Description => throw new NotImplementedException();

            public IEnumerable<IUIThreadOperationScope> Scopes => throw new NotImplementedException();

            public PropertyCollection Properties => throw new NotImplementedException();
        }
    }
}
