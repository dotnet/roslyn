// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineErrors
{
    internal sealed class ClassificationTypeDefinitions
    {
        [Export]
        [Name("IE: " + PredefinedErrorTypeNames.SyntaxError)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition InlineErrorsErrorTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [Name("IE: " + PredefinedErrorTypeNames.SyntaxError)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class InlineErrorsErrorFormatDefinition : EditorFormatDefinition
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public InlineErrorsErrorFormatDefinition()
            {
                DisplayName = EditorFeaturesResources.Inline_Errors_Error;
                BackgroundBrush = new SolidColorBrush(Color.FromRgb(252, 62, 54));
                ForegroundBrush = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            }
        }

        [Export]
        [Name("IE: " + PredefinedErrorTypeNames.Warning)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition InlineErrorsWarningTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [Name("IE: " + PredefinedErrorTypeNames.Warning)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class InlineErrorsWarningFormatDefinition : EditorFormatDefinition
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public InlineErrorsWarningFormatDefinition()
            {
                DisplayName = EditorFeaturesResources.Inline_Errors_Warning;
                BackgroundBrush = new SolidColorBrush(Color.FromRgb(149, 219, 125));
                ForegroundBrush = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            }
        }

        [Export]
        [Name("IE: " + EditAndContinueErrorTypeDefinition.Name)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition InlineErrorsRudeEditTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [Name("IE: " + EditAndContinueErrorTypeDefinition.Name)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class InlineErrorsRudeEditFormatDefinition : EditorFormatDefinition
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public InlineErrorsRudeEditFormatDefinition()
            {
                DisplayName = EditorFeaturesResources.Inline_Errors_Rude_Edit;
                BackgroundBrush = Brushes.Purple;
                ForegroundBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            }
        }
    }
}
