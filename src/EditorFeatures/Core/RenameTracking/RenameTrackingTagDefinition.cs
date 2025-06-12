// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

[Export(typeof(EditorFormatDefinition))]
[Name(RenameTrackingTag.TagId)]
[UserVisible(true)]
[ExcludeFromCodeCoverage]
internal sealed class RenameTrackingTagDefinition : MarkerFormatDefinition
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RenameTrackingTagDefinition()
    {
        this.Border = new Pen(Brushes.Black, thickness: 1.0) { DashStyle = new DashStyle(new[] { 0.5, 4.0 }, 1) };
        this.DisplayName = EditorFeaturesResources.Rename_Tracking;
        this.ZOrder = 1;
    }
}
