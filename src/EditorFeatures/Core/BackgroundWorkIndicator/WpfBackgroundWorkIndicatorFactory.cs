// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

[Export(typeof(WpfBackgroundWorkIndicatorFactory))]
[ExportWorkspaceService(typeof(IBackgroundWorkIndicatorFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class WpfBackgroundWorkIndicatorFactory(
    IThreadingContext threadingContext,
    IBackgroundWorkIndicatorService backgroundWorkIndicatorService) : IBackgroundWorkIndicatorFactory
{
    private readonly object _gate = new();
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IBackgroundWorkIndicatorService _backgroundWorkIndicatorService = backgroundWorkIndicatorService;

    private BackgroundWorkIndicatorContext? _currentContext;

    IBackgroundWorkIndicatorContext IBackgroundWorkIndicatorFactory.Create(
        ITextView textView,
        SnapshotSpan applicableToSpan,
        string description,
        bool cancelOnEdit,
        bool cancelOnFocusLost)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        // Note: this lock should be very shortly held.  We just want to ensure consistent state of _currentContext as
        // we dismiss and old one and create a new one.  This makes sure that if some feature finishes with its context
        // and disposes it in the BG, that the call into OnContextDisposed sees everything in a consistent state.
        //
        // As Create is only called on the UI thread, this can't hold up other Create calls.  So in the worse case, this
        // will only have to wait for a BG thread to do the check/assignment in the lock in OnContextDisposed, which
        // should be very fast.
        lock (_gate)
        {
            // If we have an outstanding context in flight, cancel it and create a new one to show the user.
            _currentContext?.CancelAndDispose();

            _currentContext = new BackgroundWorkIndicatorContext(this, textView, applicableToSpan, description, cancelOnEdit, cancelOnFocusLost);
            return _currentContext;
        }
    }

    private void OnContextDisposed(BackgroundWorkIndicatorContext context)
    {
        lock (_gate)
        {
            if (_currentContext == context)
                _currentContext = null;
        }
    }
}
