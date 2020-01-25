﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
