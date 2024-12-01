// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal sealed class ClassificationTypeDefinitions
{
    // Only used for theming, does not need localized
    public const string InlineRenameField = "Inline Rename Field Text";

    [Export]
    [Name(InlineRenameField)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    internal readonly ClassificationTypeDefinition? InlineRenameFieldTypeDefinition;
}
