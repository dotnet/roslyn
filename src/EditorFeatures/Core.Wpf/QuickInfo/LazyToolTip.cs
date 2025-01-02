// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal partial class ContentControlService
    {
        /// <summary>
        /// Class which allows us to provide a delay-created tooltip for our reference entries.
        /// </summary>
        private sealed class LazyToolTip
        {
            private readonly Func<DisposableToolTip> _createToolTip;
            private readonly IThreadingContext _threadingContext;
            private readonly FrameworkElement _element;

            private DisposableToolTip _disposableToolTip;

            private LazyToolTip(
                IThreadingContext threadingContext,
                FrameworkElement element,
                Func<DisposableToolTip> createToolTip)
            {
                _threadingContext = threadingContext;
                _element = element;
                _createToolTip = createToolTip;

                _threadingContext.ThrowIfNotOnUIThread();

                // Set ourselves as the tooltip of this text block.  This will let WPF know that 
                // it should attempt to show tooltips here.  When WPF wants to show the tooltip 
                // though we'll hear about it "ToolTipOpening".  When that happens, we'll swap
                // out ourselves with a real tooltip that is lazily created.  When that tooltip
                // is the dismissed, we'll release the resources associated with it and we'll
                // reattach ourselves.
                _element.ToolTip = this;

                element.ToolTipOpening += this.OnToolTipOpening;
                element.ToolTipClosing += this.OnToolTipClosing;
            }

            public static void AttachTo(FrameworkElement element, IThreadingContext threadingContext, Func<DisposableToolTip> createToolTip)
                => new LazyToolTip(threadingContext, element, createToolTip);

            private void OnToolTipOpening(object sender, ToolTipEventArgs e)
            {
                try
                {
                    _threadingContext.ThrowIfNotOnUIThread();

                    Debug.Assert(_element.ToolTip == this);
                    Debug.Assert(_disposableToolTip == null);

                    _disposableToolTip = _createToolTip();
                    _element.ToolTip = _disposableToolTip.ToolTip;
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                {
                    // Do nothing, since this is a WPF event handler and propagating the exception would cause a crash
                }
            }

            private void OnToolTipClosing(object sender, ToolTipEventArgs e)
            {
                try
                {
                    _threadingContext.ThrowIfNotOnUIThread();

                    Debug.Assert(_disposableToolTip != null);
                    Debug.Assert(_element.ToolTip == _disposableToolTip.ToolTip);

                    _element.ToolTip = this;

                    _disposableToolTip.Dispose();
                    _disposableToolTip = null;
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                {
                    // Do nothing, since this is a WPF event handler and propagating the exception would cause a crash
                }
            }
        }
    }
}
