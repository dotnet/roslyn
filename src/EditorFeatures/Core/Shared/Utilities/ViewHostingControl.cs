// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal class ViewHostingControl : ContentControl
    {
        private readonly Func<ITextBuffer, IWpfTextView> _createView;
        private readonly Func<ITextBuffer> _createBuffer;

        public ViewHostingControl(
            Func<ITextBuffer, IWpfTextView> createView, 
            Func<ITextBuffer> createBuffer)
        {
            _createView = createView;
            _createBuffer = createBuffer;

            Background = Brushes.Transparent;
            this.IsVisibleChanged += OnIsVisibleChanged;
        }

        public ITextView TextView_TestOnly
        {
            get
            {
                var view = (IWpfTextView)this.Content;
                if (view == null)
                {
                    view = _createView(_createBuffer());
                    this.Content = view.VisualElement;
                }

                return view;
            }
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var nowVisible = (bool)e.NewValue;
            if (nowVisible)
            {
                if (this.Content == null)
                {
                    this.Content = _createView(_createBuffer()).VisualElement;
                }
            }
            else
            {
                ((ITextView)this.Content).Close();
                this.Content = null;
            }
        }

        public override string ToString()
        {
            if (this.Content != null)
            {
                return ((ITextView)this.Content).TextBuffer.CurrentSnapshot.GetText();
            }

            return _createBuffer().CurrentSnapshot.GetText();
        }
    }
}
