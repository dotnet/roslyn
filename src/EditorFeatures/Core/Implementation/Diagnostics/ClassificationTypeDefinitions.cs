// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
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
