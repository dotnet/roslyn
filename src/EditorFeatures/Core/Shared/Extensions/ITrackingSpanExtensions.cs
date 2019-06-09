// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class ITrackingSpanExtensions
    {
        public static ITrackingPoint GetStartTrackingPoint(this ITrackingSpan span, PointTrackingMode mode)
        {
            return span.GetStartPoint(span.TextBuffer.CurrentSnapshot).CreateTrackingPoint(mode);
        }
    }
}
