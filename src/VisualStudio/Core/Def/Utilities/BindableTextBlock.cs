// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal class BindableTextBlock : TextBlock
    {
        public IList<Inline> InlineCollection
        {
            get { return (ObservableCollection<Inline>)GetValue(InlineListProperty); }
            set { SetValue(InlineListProperty, value); }
        }

        public static readonly DependencyProperty InlineListProperty =
            DependencyProperty.Register(nameof(InlineCollection), typeof(IList<Inline>), typeof(BindableTextBlock), new UIPropertyMetadata(null, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = (BindableTextBlock)sender;
            var newList = (IList<Inline>)e.NewValue;

            textBlock.Inlines.Clear();

            if (newList is null)
            {
                return;
            }

            foreach (var inline in newList)
            {
                textBlock.Inlines.Add(inline);
            }
        }
    }
}
