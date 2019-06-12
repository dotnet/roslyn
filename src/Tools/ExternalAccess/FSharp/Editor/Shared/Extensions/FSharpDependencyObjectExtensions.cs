// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Shared.Extensions
{
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
}
