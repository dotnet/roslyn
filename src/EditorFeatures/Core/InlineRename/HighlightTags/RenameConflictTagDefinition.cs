// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;

[Export(typeof(EditorFormatDefinition))]
[Name(RenameConflictTag.TagId)]
[UserVisible(true)]
[ExcludeFromCodeCoverage]
internal sealed class RenameConflictTagDefinition : MarkerFormatDefinition
{
    public static double StrokeThickness => 1.5;
    public static double[] StrokeDashArray => [8.0, 4.0];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RenameConflictTagDefinition()
    {
        this.Border = new Pen(Brushes.Red, thickness: StrokeThickness) { DashStyle = new DashStyle(StrokeDashArray, 0) };
        this.DisplayName = EditorFeaturesResources.Inline_Rename_Conflict;
        this.ZOrder = 10;
    }
}
