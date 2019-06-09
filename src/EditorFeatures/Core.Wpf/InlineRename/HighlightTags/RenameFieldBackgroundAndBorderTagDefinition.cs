// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(RenameFieldBackgroundAndBorderTag.TagId)]
    [UserVisible(true)]
    [ExcludeFromCodeCoverage]
    internal class RenameFieldBackgroundAndBorderTagDefinition : MarkerFormatDefinition
    {
        [ImportingConstructor]
        public RenameFieldBackgroundAndBorderTagDefinition()
        {
            // The Border color should match the BackgroundColor from the
            // InlineRenameFieldFormatDefinition.
            this.Border = new Pen(new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)), thickness: 2.0);
            this.BackgroundColor = Color.FromRgb(0xA6, 0xF1, 0xA6);
            this.DisplayName = EditorFeaturesResources.Inline_Rename_Field_Background_and_Border;

            // Needs to show above highlight references, but below the resolved/unresolved rename 
            // conflict tags.
            this.ZOrder = 1;
        }
    }
}
