// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
