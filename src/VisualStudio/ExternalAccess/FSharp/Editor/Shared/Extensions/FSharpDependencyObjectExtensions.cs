// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Shared.Extensions;

internal static class FSharpDependencyObjectExtensions
{
    public static void SetTextProperties(this DependencyObject dependencyObject, TextFormattingRunProperties textProperties)
    {
        DependencyObjectExtensions.SetTextProperties(dependencyObject, textProperties);
    }

    public static void SetDefaultTextProperties(this DependencyObject dependencyObject, IClassificationFormatMap formatMap)
    {
        DependencyObjectExtensions.SetDefaultTextProperties(dependencyObject, formatMap);
    }
}
