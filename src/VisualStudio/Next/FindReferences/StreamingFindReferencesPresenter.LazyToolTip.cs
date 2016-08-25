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
        private class LazyToolTip
        {
            private readonly ForegroundThreadAffinitizedObject _foregroundObject = new ForegroundThreadAffinitizedObject();
            private readonly Func<DisposableToolTip> _createToolTip;
            private readonly TextBlock _textBlock;

            private DisposableToolTip _disposableToolTip;

            private LazyToolTip(
                TextBlock textBlock,
                Func<DisposableToolTip> createToolTip)
            {
                _foregroundObject.AssertIsForeground();

                _textBlock = textBlock;
                _createToolTip = createToolTip;

                // Set ourselves as the tooltip of this text block.  This will let WPF know that 
                // it should attempt to show tooltips here.  When WPF wants to show the tooltip 
                // though we'll hear about it "ToolTipOpening".  When that happens, we'll swap
                // out ourselves with a real tooltip that is lazily created.  When that tooltip
                // is the dismissed, we'll release the resources associated with it and we'll
                // reattach ourselves.
                _textBlock.ToolTip = this;

                textBlock.ToolTipOpening += this.OnToolTipOpening;
                textBlock.ToolTipClosing += this.OnToolTipClosing;
            }

            public static void AttachTo(TextBlock textBlock, Func<DisposableToolTip> createToolTip)
            {
                new LazyToolTip(textBlock, createToolTip);
            }

            private void OnToolTipOpening(object sender, ToolTipEventArgs e)
            {
                _foregroundObject.AssertIsForeground();

                Debug.Assert(_textBlock.ToolTip == this);
                Debug.Assert(_disposableToolTip == null);

                _disposableToolTip = _createToolTip();
                _textBlock.ToolTip = _disposableToolTip.ToolTip;
            }

            private void OnToolTipClosing(object sender, ToolTipEventArgs e)
            {
                _foregroundObject.AssertIsForeground();

                Debug.Assert(_disposableToolTip != null);
                Debug.Assert(_textBlock.ToolTip == _disposableToolTip.ToolTip);

                _textBlock.ToolTip = this;

                _disposableToolTip.Dispose();
                _disposableToolTip = null;
            }
        }
    }
}