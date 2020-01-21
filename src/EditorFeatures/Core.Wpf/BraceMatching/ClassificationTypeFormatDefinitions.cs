// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    internal sealed class ClassificationTypeFormatDefinitions
    {
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeDefinitions.BraceMatchingName)]
        [Name(ClassificationTypeDefinitions.BraceMatchingName)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        private class BraceMatchingFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public BraceMatchingFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Brace_Matching;
                this.BackgroundColor = Color.FromRgb(0xDB, 0xE0, 0xCC);
            }
        }
    }
}
