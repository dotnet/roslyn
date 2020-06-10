// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptSignatureHelpClassifierFactory
    {
        public static IClassifier Create(ITextBuffer textBuffer, ClassificationTypeMap typeMap)
            => new SignatureHelpClassifier(textBuffer, typeMap);
    }
}
