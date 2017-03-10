// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    // This class is necessary so that our rename colors work on the
    // dark theme as well.  Otherwise the default white identifier 
    // foreground color clashes with our light green background.
    internal sealed class ClassificationTypeDefinitions
    {
        public const string InlineRenameField = "inline rename field";

        [Export]
        [Name(InlineRenameField)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition InlineRenameFieldTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = InlineRenameField)]
        [Name(InlineRenameField)]
        [Order(After = Priority.High)]
        [UserVisible(false)]
        private class InlineRenameFieldFormatDefinition : ClassificationFormatDefinition
        {
            private InlineRenameFieldFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Inline_Rename;
                this.ForegroundColor = Color.FromRgb(0, 100, 0);
            }
        }
    }
}
