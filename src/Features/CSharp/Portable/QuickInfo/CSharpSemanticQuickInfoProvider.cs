// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(QuickInfoProviderNames.Semantic, LanguageNames.CSharp), Shared]
    internal class CSharpSemanticQuickInfoProvider : CommonSemanticQuickInfoProvider
    {
        protected override bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return !token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);
        }
    }
}
