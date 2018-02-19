// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        /// <summary>
        /// Class which allows us to provide a delay-created tooltip for our reference entries.
        /// </summary>
        private class LazyToolTip
        {
            private readonly ForegroundThreadAffinitizedObject _foregroundObject = new ForegroundThreadAffinitizedObject();
            private readonly Func<DisposableToolTip> _createToolTip;
            private readonly FrameworkElement _element;

            private DisposableToolTip _disposableToolTip;

            private LazyToolTip(
                FrameworkElement element,
                Func<DisposableToolTip> createToolTip)
            {
                _foregroundObject.AssertIsForeground();

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

            public static void AttachTo(FrameworkElement element, Func<DisposableToolTip> createToolTip)
            {
                new LazyToolTip(element, createToolTip);
            }

            private void OnToolTipOpening(object sender, ToolTipEventArgs e)
            {
                _foregroundObject.AssertIsForeground();

                Debug.Assert(_element.ToolTip == this);
                Debug.Assert(_disposableToolTip == null);

                _disposableToolTip = _createToolTip();
                _element.ToolTip = _disposableToolTip.ToolTip;
            }

            private void OnToolTipClosing(object sender, ToolTipEventArgs e)
            {
                _foregroundObject.AssertIsForeground();

                Debug.Assert(_disposableToolTip != null);
                Debug.Assert(_element.ToolTip == _disposableToolTip.ToolTip);

                _element.ToolTip = this;

                _disposableToolTip.Dispose();
                _disposableToolTip = null;
            }
        }
    }
}
