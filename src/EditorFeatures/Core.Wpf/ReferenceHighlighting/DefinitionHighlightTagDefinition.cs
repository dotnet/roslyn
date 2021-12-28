// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(DefinitionHighlightTag.TagId)]
    [UserVisible(true)]
    internal class DefinitionHighlightTagDefinition : MarkerFormatDefinition
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefinitionHighlightTagDefinition()
        {
            // NOTE: This is the same color used by the editor for reference highlighting
            this.BackgroundColor = Color.FromRgb(219, 224, 204);
            this.Border = new Pen(new SolidColorBrush(Colors.Gray), 0.5);
            this.Border.Freeze();
            this.DisplayName = EditorFeaturesResources.Highlighted_Definition;
        }
    }
}
