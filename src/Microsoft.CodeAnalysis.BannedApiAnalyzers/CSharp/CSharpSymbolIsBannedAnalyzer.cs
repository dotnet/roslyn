// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.BannedApiAnalyzers;
using System.Collections.Immutable;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpSymbolIsBannedAnalyzer : SymbolIsBannedAnalyzer<SyntaxKind>
    {
        protected override SyntaxKind XmlCrefSyntaxKind => SyntaxKind.XmlCrefAttribute;

        protected override ImmutableArray<SyntaxKind> BaseTypeSyntaxKinds => ImmutableArray.Create(SyntaxKind.BaseList);

        protected override SymbolDisplayFormat SymbolDisplayFormat => SymbolDisplayFormat.CSharpShortErrorMessageFormat;

        protected override SyntaxNode GetReferenceSyntaxNodeFromXmlCref(SyntaxNode syntaxNode) => ((XmlCrefAttributeSyntax)syntaxNode).Cref;

        protected override IEnumerable<SyntaxNode> GetTypeSyntaxNodesFromBaseType(SyntaxNode syntaxNode) => ((BaseListSyntax)syntaxNode).Types.Select(t => (SyntaxNode)t.Type);
    }
}