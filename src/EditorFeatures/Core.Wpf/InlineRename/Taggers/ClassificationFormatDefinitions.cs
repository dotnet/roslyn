// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal sealed class ClassificationFormatDefinitions
    {
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeDefinitions.InlineRenameField)]
        [Name(ClassificationTypeDefinitions.InlineRenameField)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        private class InlineRenameFieldFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public InlineRenameFieldFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Inline_Rename_Field_Text;
                this.ForegroundColor = Color.FromRgb(0x00, 0x64, 0x00);
            }
        }
    }
}
