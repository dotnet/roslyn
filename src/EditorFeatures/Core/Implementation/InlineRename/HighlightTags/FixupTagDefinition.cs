// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(FixupTag.TagId)]
    [UserVisible(true)]
    [ExcludeFromCodeCoverage]
    internal class FixupTagDefinition : MarkerFormatDefinition
    {
        public FixupTagDefinition()
        {
            this.Border = new Pen(Brushes.Green, thickness: 1.0) { DashStyle = new DashStyle(new[] { 4.0, 4.0 }, 0) };
            this.DisplayName = EditorFeaturesResources.Inline_Rename_Fixup;
            this.ZOrder = 1;
        }
    }
}
