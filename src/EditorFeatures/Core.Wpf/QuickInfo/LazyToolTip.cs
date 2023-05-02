// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal partial class ContentControlService
    {
        /// <summary>
        /// Class which allows us to provide a delay-created tooltip for our reference entries.
        /// </summary>
        private class LazyToolTip : ForegroundThreadAffinitizedObject, IDisposable
        {
            private readonly Func<ReferenceCountedDisposable<DisposableToolTip>> _createToolTip;
            private readonly FrameworkElement _element;

            private ReferenceCountedDisposable<DisposableToolTip> _disposableToolTip;

            private LazyToolTip(
                IThreadingContext threadingContext,
                FrameworkElement element,
                Func<ReferenceCountedDisposable<DisposableToolTip>> createToolTip)
                : base(threadingContext, assertIsForeground: true)
            {
                _element = element;
                _createToolTip = createToolTip;

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

            public static IDisposable AttachTo(FrameworkElement element, IThreadingContext threadingContext, Func<ReferenceCountedDisposable<DisposableToolTip>> createToolTip)
                => new LazyToolTip(threadingContext, element, createToolTip);

            public void Dispose()
            {
                try
                {
                    Interlocked.Exchange(ref _disposableToolTip, null)?.Dispose();
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                {
                    // Avoid propagating the exception within a UI cleanup layer
                }
            }

            private void OnToolTipOpening(object sender, ToolTipEventArgs e)
            {
                try
                {
                    AssertIsForeground();

                    Debug.Assert(_element.ToolTip == this);
                    Debug.Assert(_disposableToolTip == null);

                    // We don't expect _disposableToolTip to be non-null here, but we still make sure it's not leaking
                    if (_disposableToolTip is not null)
                        return;

                    using var disposableToolTip = _createToolTip();
                    _disposableToolTip = disposableToolTip.AddReference();
                    _element.ToolTip = disposableToolTip.Target.ToolTip;
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
                    AssertIsForeground();

                    Debug.Assert(_disposableToolTip != null);
                    Debug.Assert(_element.ToolTip == _disposableToolTip.Target.ToolTip);

                    _element.ToolTip = this;

                    // Handle the case where the tool tip was disposed before calling OnToolTipClosing
                    _disposableToolTip?.Dispose();
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
