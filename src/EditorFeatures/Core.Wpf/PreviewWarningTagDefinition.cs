// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging.Tags
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(PreviewWarningTag.TagId)]
    [UserVisible(true)]
    [ExcludeFromCodeCoverage]
    internal class PreviewWarningTagDefinition : MarkerFormatDefinition
    {
        public PreviewWarningTagDefinition()
        {
            // this.Border = new Pen(Brushes.Yellow, thickness: 1.5);
            this.BackgroundColor = Colors.Yellow;
            this.DisplayName = EditorFeaturesResources.Preview_Warning;
            this.ZOrder = 10;
        }
    }
}
