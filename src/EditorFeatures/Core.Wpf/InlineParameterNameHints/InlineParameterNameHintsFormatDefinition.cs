// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    [Export(typeof(EditorFormatDefinition))]
    internal sealed class InlineParameterNameHintsFormatDefinition : EditorFormatDefinition
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineParameterNameHintsFormatDefinition()
        {
            this.DisplayName = EditorFeaturesResources.Inline_Parameter_Name_Hints;
            this.ForegroundBrush = Brushes.Black;
            this.BackgroundBrush = Brushes.LightGray;
        }

    }
}
