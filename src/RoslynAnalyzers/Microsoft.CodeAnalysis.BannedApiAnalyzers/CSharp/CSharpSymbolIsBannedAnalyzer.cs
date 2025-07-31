// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.BannedApiAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
