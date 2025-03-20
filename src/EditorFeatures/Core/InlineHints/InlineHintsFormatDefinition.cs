// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    internal sealed class ClassificationTypeDefinitions
    {
        [Export]
        [Name(InlineHintsTag.TagId)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition InlineHints;

        [Export(typeof(EditorFormatDefinition))]
        [Name(InlineHintsTag.TagId)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        internal sealed class InlineHintsFormatDefinition : EditorFormatDefinition
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public InlineHintsFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Inline_Hints;
                this.ForegroundBrush = new SolidColorBrush(Color.FromRgb(104, 104, 104));
                this.BackgroundBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            }
        }
    }
}
