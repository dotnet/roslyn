// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class ClassificationTypeDefinitions
    {
        public const string UnnecessaryCode = "unnecessary code";

        [Export]
        [Name(UnnecessaryCode)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition UnnecessaryCodeTypeDefinition { get; set; }
    }
}
