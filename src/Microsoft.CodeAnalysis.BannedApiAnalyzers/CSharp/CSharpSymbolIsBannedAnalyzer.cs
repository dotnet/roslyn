// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.BannedApiAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpSymbolIsBannedAnalyzer : SymbolIsBannedAnalyzer<SyntaxKind>
    {
        protected override SyntaxKind XmlCrefSyntaxKind => SyntaxKind.XmlCrefAttribute;

        protected override SymbolDisplayFormat SymbolDisplayFormat => SymbolDisplayFormat.CSharpShortErrorMessageFormat;

        protected override SyntaxNode GetReferenceSyntaxNodeFromXmlCref(SyntaxNode syntaxNode) => ((XmlCrefAttributeSyntax)syntaxNode).Cref;
    }
}