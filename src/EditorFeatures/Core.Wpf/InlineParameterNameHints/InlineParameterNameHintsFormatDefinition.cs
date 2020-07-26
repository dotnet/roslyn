// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    internal sealed class ClassificationTypeDefinitions
    {
        [Export]
        [Name(InlineParameterNameHintsTag.TagId)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition InlineParameterNameHints;

        [Export(typeof(EditorFormatDefinition))]
        [Name(InlineParameterNameHintsTag.TagId)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
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
}
