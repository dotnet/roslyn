// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    [ExportQuickInfoElementProvider("Semantic", LanguageNames.CSharp), Shared]
    internal class CSharpSemanticQuickInfoElementProvider : CommonSemanticQuickInfoElementProvider
    {
        public CSharpSemanticQuickInfoElementProvider()
        {
        }

        protected override bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return !token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);
        }
    }
}
