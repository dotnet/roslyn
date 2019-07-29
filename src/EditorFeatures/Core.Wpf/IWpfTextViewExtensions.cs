// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IWpfTextViewExtensions
    {
        public static void SizeToFit(this IWpfTextView view, IThreadingContext threadingContext)
        {
            void firstLayout(object sender, TextViewLayoutChangedEventArgs args)
            {
                threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);

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
                });

                view.LayoutChanged -= firstLayout;
            }

            view.LayoutChanged += firstLayout;

            static bool IsGreater(double value, double other)
                => IsNormal(value) && (!IsNormal(other) || value > other);

            static bool IsNormal(double value)
                => !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
