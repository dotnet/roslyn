// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueErrorTypeFormatDefinition()
        {
            this.ForegroundBrush = Brushes.Purple;
            this.BackgroundCustomizable = false;
            this.DisplayName = EditorFeaturesResources.Rude_Edit;
        }
    }
}
