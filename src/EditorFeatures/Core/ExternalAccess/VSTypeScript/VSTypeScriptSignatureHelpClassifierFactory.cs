// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    internal static class VSTypeScriptSignatureHelpClassifierFactory
    {
        public static IClassifier Create(ITextBuffer textBuffer, ClassificationTypeMap typeMap)
            => new SignatureHelpClassifier(textBuffer, typeMap);
    }
}
