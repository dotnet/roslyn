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

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging.Tags;

[Export(typeof(EditorFormatDefinition))]
[Name(PreviewWarningTag.TagId)]
[UserVisible(true)]
[ExcludeFromCodeCoverage]
internal sealed class PreviewWarningTagDefinition : MarkerFormatDefinition
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PreviewWarningTagDefinition()
    {
        this.Border = new Pen(new SolidColorBrush(Color.FromRgb(230, 117, 64)), thickness: 1.5);
        this.DisplayName = EditorFeaturesResources.Preview_Warning;
        this.ZOrder = 10;
    }
}
