// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(RenameTrackingTag.TagId)]
    [UserVisible(true)]
    [ExcludeFromCodeCoverage]
    internal class RenameTrackingTagDefinition : MarkerFormatDefinition
    {
        [ImportingConstructor]
        public RenameTrackingTagDefinition()
        {
            this.Border = new Pen(Brushes.Black, thickness: 1.0) { DashStyle = new DashStyle(new[] { 0.5, 4.0 }, 1) };
            this.DisplayName = EditorFeaturesResources.Rename_Tracking;
            this.ZOrder = 1;
        }
    }
}
