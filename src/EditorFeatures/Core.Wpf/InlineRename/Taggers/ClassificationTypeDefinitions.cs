// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal sealed class ClassificationTypeDefinitions
    {
        // Only used for theming, does not need localized
        public const string InlineRenameField = "Inline Rename Field Text";

        [Export]
        [Name(InlineRenameField)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition InlineRenameFieldTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = InlineRenameField)]
        [Name(InlineRenameField)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        private class InlineRenameFieldFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public InlineRenameFieldFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Inline_Rename_Field_Text;
                this.ForegroundColor = Color.FromRgb(0x00, 0x64, 0x00);
            }
        }
    }
}
