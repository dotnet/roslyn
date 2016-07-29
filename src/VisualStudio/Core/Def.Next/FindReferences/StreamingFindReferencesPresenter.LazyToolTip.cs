// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        /// <summary>
        /// Class which allows us to provide a delay-created tooltip for our reference entries.
        /// </summary>
        private class LazyTip : ContentControl
        {
            private readonly ForegroundThreadAffinitizedObject _foregroundObject = new ForegroundThreadAffinitizedObject();
            private readonly Func<DisposableToolTip> _createToolTip;
            private readonly FrameworkElement _element;

            private DisposableToolTip _disposableToolTip;

            public LazyTip(FrameworkElement element,
                Func<DisposableToolTip> createToolTip)
            {
                _foregroundObject.AssertIsForeground();

                _element = element;
                _createToolTip = createToolTip;
                Background = Brushes.Transparent;

                element.ToolTipOpening += this.OnToolTipOpening;
                element.ToolTipClosing += this.OnToolTipClosing;
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