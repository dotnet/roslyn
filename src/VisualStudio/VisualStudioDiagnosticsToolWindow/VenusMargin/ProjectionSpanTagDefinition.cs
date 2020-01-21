// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Hosting.Diagnostics.VenusMargin
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(ProjectionSpanTag.TagId)]
    internal class ProjectionSpanTagDefinition : MarkerFormatDefinition
    {
        [ImportingConstructor]
        public ProjectionSpanTagDefinition()
        {
            this.Border = new Pen(Brushes.DarkBlue, thickness: 1.5);
            this.BackgroundColor = Colors.LightBlue;
            this.DisplayName = "Projection Span";
            this.ZOrder = 10;
        }
    }
}
