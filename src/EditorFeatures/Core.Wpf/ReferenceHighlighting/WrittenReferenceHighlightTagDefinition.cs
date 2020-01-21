// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(WrittenReferenceHighlightTag.TagId)]
    [UserVisible(true)]
    internal class WrittenReferenceHighlightTagDefinition : MarkerFormatDefinition
    {
        [ImportingConstructor]
        public WrittenReferenceHighlightTagDefinition()
        {
            // NOTE: This is the same color used by the editor for reference highlighting
            this.BackgroundColor = Color.FromRgb(219, 224, 204);
            this.DisplayName = EditorFeaturesResources.Highlighted_Written_Reference;
        }
    }
}
