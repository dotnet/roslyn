// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(EditAndContinueErrorTypeDefinition.Name)]
    [UserVisible(true)]
    internal sealed class EditAndContinueErrorTypeFormatDefinition : EditorFormatDefinition
    {
        [ImportingConstructor]
        public EditAndContinueErrorTypeFormatDefinition()
        {
            this.ForegroundBrush = Brushes.Purple;
            this.BackgroundCustomizable = false;
            this.DisplayName = EditorFeaturesResources.Rude_Edit;
        }
    }
}
