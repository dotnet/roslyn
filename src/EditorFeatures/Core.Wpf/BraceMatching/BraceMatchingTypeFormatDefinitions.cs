// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching;

internal sealed class BraceMatchingTypeFormatDefinitions
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ClassificationTypeDefinitions.BraceMatchingName)]
    [Name(ClassificationTypeDefinitions.BraceMatchingName)]
    [Order(After = Priority.High)]
    [UserVisible(true)]
    private sealed class BraceMatchingFormatDefinition : ClassificationFormatDefinition
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BraceMatchingFormatDefinition()
        {
            this.DisplayName = EditorFeaturesResources.Brace_Matching;
            this.BackgroundColor = Color.FromRgb(0xDB, 0xE0, 0xCC);
        }
    }
}
