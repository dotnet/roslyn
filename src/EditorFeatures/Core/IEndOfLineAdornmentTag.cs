// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IEndOfLineAdornmentTag : ITag
    {
        string Type { get; }
        double Width { get; }
        double Height { get; }
        double HorizontalOffset { get; }
        double VerticalOffset { get; }
    }
}
