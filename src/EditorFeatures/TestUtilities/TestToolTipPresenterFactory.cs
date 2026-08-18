// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

// The editor's BackgroundWorkIndicator (and EventHookup) eagerly create a tooltip via IToolTipPresenterFactory.
// The real WPF presenter factory's imports (the WPF view-element/tooltip-style services) are not composed in the
// test catalog, so MEF rejects it and ToolTipService.CreatePresenter fails with "No applicable IToolTipPresenterFactory".
// Export a no-op presenter factory so tests that exercise the background work indicator can run headless.
[Export(typeof(IToolTipPresenterFactory))]
[Name("test")]
[Order(Before = "default")]
internal sealed class TestToolTipPresenterFactory : IToolTipPresenterFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestToolTipPresenterFactory()
    {
    }

    public IToolTipPresenter Create(ITextView textView, ToolTipParameters parameters)
        => new NoOpToolTipPresenter();

    private sealed class NoOpToolTipPresenter : IToolTipPresenter
    {
        public event EventHandler Dismissed { add { } remove { } }

        public void StartOrUpdate(ITrackingSpan applicableToSpan, IEnumerable<object> content)
        {
        }

        public void Dismiss()
        {
        }
    }
}
