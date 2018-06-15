// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IWpfTextViewExtensions
    {
        public static void SizeToFit(this IWpfTextView view)
        {
            void firstLayout(object sender, TextViewLayoutChangedEventArgs args)
            {
                view.VisualElement.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var newHeight = view.LineHeight * view.TextBuffer.CurrentSnapshot.LineCount;             
                    if (IsGreater(newHeight, view.VisualElement.Height))
                    {
                        view.VisualElement.Height = newHeight;
                    }

                    var newWidth = view.MaxTextRightCoordinate;
                    if (IsGreater(newWidth, view.VisualElement.Width))
                    {
                        view.VisualElement.Width = newWidth;
                    }
                }));                
                view.LayoutChanged -= firstLayout;
            }
            view.LayoutChanged += firstLayout;
            
            bool IsGreater(double value, double other)
                => IsNormal(value) && (!IsNormal(other) || value > other);

            bool IsNormal(double value)
                => !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
