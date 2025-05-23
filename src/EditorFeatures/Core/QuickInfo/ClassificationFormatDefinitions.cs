// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo;

internal sealed class ClassificationFormatDefinitions
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ClassificationTypeDefinitions.ReducedEmphasisText)]
    [Name(ClassificationTypeDefinitions.ReducedEmphasisText)]
    [Order(After = Priority.High)]
    [UserVisible(false)]
    private sealed class ReducedEmphasisTextFormat : ClassificationFormatDefinition
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ReducedEmphasisTextFormat()
        {
            this.ForegroundOpacity = 0.65f;
        }
    }
}

internal sealed class ClassificationTypeDefinitions
{
    // Only used for theming, does not need localized
    public const string ReducedEmphasisText = "Reduced Emphasis Text";

    [Export]
    [Name(ReducedEmphasisText)]
    [BaseDefinition(PredefinedClassificationTypeNames.Text)]
    internal readonly ClassificationTypeDefinition? ReducedEmphasisTextTypeDefinition;
}
