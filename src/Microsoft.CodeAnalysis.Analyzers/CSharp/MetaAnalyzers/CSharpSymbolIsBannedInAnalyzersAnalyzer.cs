// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Analyzers;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpSymbolIsBannedInAnalyzersAnalyzer : SymbolIsBannedInAnalyzersAnalyzer<SyntaxKind>
    {
        protected override SyntaxKind XmlCrefSyntaxKind => SyntaxKind.XmlCrefAttribute;

        protected override SymbolDisplayFormat SymbolDisplayFormat => SymbolDisplayFormat.CSharpShortErrorMessageFormat;

        protected override SyntaxNode GetReferenceSyntaxNodeFromXmlCref(SyntaxNode syntaxNode) => ((XmlCrefAttributeSyntax)syntaxNode).Cref;
    }
}