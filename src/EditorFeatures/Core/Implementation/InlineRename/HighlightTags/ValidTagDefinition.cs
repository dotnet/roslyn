// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(ValidTag.TagId)]
    [UserVisible(true)]
    [ExcludeFromCodeCoverage]
    internal class ValidTagDefinition : MarkerFormatDefinition
    {
        public ValidTagDefinition()
        {
            this.Border = new Pen(Brushes.LightGreen, thickness: 0.5);
            this.BackgroundColor = Colors.LightGreen;
            this.DisplayName = EditorFeaturesResources.Inline_Rename;
            this.ZOrder = 5;
        }
    }
}
