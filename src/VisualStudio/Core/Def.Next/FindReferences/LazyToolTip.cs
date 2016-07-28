// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal class LazyTip : ContentControl
    {
        private readonly Func<ToolTip> _createToolTip;
        private readonly FrameworkElement _element;
        private readonly Action _refreshContent;

        public LazyTip(FrameworkElement element, Func<ToolTip> createToolTip, Action refreshContent)
        {
            _element = element;
            _createToolTip = createToolTip;
            _refreshContent = refreshContent;

            element.ToolTipOpening += this.OnToolTipOpening;
            Background = Brushes.Transparent;
        }

        private void OnToolTipOpening(object sender, ToolTipEventArgs e)
        {
            // _element.ToolTipOpening -= this.OnToolTipOpening;

            if (_element.ToolTip == this)
            {
                _element.ToolTip = _createToolTip();
                return;
                //var content = _createContent();
                //this.Content = content;
                //return;
            }
            else
            {
                _refreshContent();
            }
        }
    }
}
