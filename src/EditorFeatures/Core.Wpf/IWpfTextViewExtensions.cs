// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IWpfTextViewExtensions
    {
        public static void SizeToFit(this IWpfTextView view)
        {
            // Computing the height of something is easy.
            view.VisualElement.Height = view.LineHeight * view.TextBuffer.CurrentSnapshot.LineCount;

            // Computing the width... less so. We need "MaxTextRightCoordinate", but we won't have
            // that until a layout occurs.  Fortunately, a layout is going to occur because we set
            // 'Height' above.
            void firstLayout(object sender, TextViewLayoutChangedEventArgs args)
            {
                view.VisualElement.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var newWidth = view.MaxTextRightCoordinate;
                    var currentWidth = view.VisualElement.Width;

                    // If the element already was given a width, then only set the width if we
                    // wouldn't make it any smaller.
                    if (IsNormal(newWidth) && IsNormal(currentWidth) && newWidth <= currentWidth)
                    {
                        return;
                    }

                    view.VisualElement.Width = view.MaxTextRightCoordinate;
                }));
            }

            view.LayoutChanged += firstLayout;
        }

        private static bool IsNormal(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
