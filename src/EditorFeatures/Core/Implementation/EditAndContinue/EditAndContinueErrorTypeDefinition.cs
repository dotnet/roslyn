// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [UserVisible(true)]
    internal sealed class EditAndContinueErrorTypeDefinition : EditorFormatDefinition
    {
        internal const string Name = "Edit and Continue";

        [Export(typeof(ErrorTypeDefinition))]
        [Name(Name)]
        internal static ErrorTypeDefinition Definition;

        public EditAndContinueErrorTypeDefinition()
        {
            this.ForegroundBrush = Brushes.Purple;
            this.BackgroundCustomizable = false;
            this.DisplayName = EditorFeaturesResources.Rude_Edit;
        }
    }
}
