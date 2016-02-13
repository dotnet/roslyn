// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Roslyn.Hosting.Diagnostics.VenusMargin
{
    public partial class ProjectionBufferMargin : UserControl
    {
        public static event EventHandler SelectionChanged;

        public IWpfTextView TextView { get; set; }
        public ITextEditorFactoryService TextEditorFactory { get; set; }

        public ProjectionBufferMargin()
        {
            InitializeComponent();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextView.Properties[ProjectionSpanTaggerProvider.PropertyName] = new List<Span>(e.AddedItems.Cast<SnapshotSpan>().Select(ss => ss.Span).Where(ss => !ss.IsEmpty));
            RaiseSelectionChanged(EventArgs.Empty);
        }

        private void RaiseSelectionChanged(EventArgs args)
        {
            SelectionChanged?.Invoke(this, args);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var buffer = (ITextBuffer)((FrameworkElement)sender).Tag;

            var view = TextEditorFactory.CreateTextView(buffer);

            var projectionBuffer = TextView.TextBuffer as IProjectionBuffer;
            var spansFromBuffer = from ss in projectionBuffer.CurrentSnapshot.GetSourceSpans()
                                  where ss.Snapshot.TextBuffer == buffer
                                  select ss.Span;

            view.Properties[ProjectionSpanTaggerProvider.PropertyName] = new List<Span>(spansFromBuffer);
            var host = TextEditorFactory.CreateTextViewHost(view, setFocus: true);

            var window = new Window
            {
                Content = host.HostControl,
                ShowActivated = true,
            };

            window.Closed += (s, a) =>
            {
                host.Close();
            };

            window.Show();
        }
    }
}
