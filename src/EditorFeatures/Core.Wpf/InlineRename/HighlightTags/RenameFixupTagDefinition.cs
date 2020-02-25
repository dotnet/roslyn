﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(RenameFixupTag.TagId)]
    [UserVisible(true)]
    [ExcludeFromCodeCoverage]
    internal class RenameFixupTagDefinition : MarkerFormatDefinition
    {
        public static double StrokeThickness => 1.0;
        public static double[] StrokeDashArray => new[] { 4.0, 4.0 };

        [ImportingConstructor]
        public RenameFixupTagDefinition()
        {
            this.Border = new Pen(Brushes.Green, thickness: StrokeThickness) { DashStyle = new DashStyle(StrokeDashArray, 0) };
            this.DisplayName = EditorFeaturesResources.Inline_Rename_Fixup;
            this.ZOrder = 1;
        }
    }
}
