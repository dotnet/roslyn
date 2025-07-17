// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.OrganizeImports;

namespace Microsoft.CodeAnalysis.CSharp.OrganizeImports;

internal partial class CSharpOrganizeImportsService
{
    private sealed class Rewriter(OrganizeImportsOptions options) : CSharpSyntaxRewriter
    {
        private readonly bool _placeSystemNamespaceFirst = options.PlaceSystemNamespaceFirst;
        private readonly bool _separateGroups = options.SeparateImportDirectiveGroups;
        private readonly SyntaxTrivia _fallbackTrivia = CSharpSyntaxGeneratorInternal.Instance.EndOfLine(options.NewLine);

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            node = (CompilationUnitSyntax)base.VisitCompilationUnit(node)!;
            UsingsAndExternAliasesOrganizer.Organize(
                node.Externs, node.Usings,
                _placeSystemNamespaceFirst, _separateGroups,
                _fallbackTrivia,
                out var organizedExternAliasList, out var organizedUsingList);

            return node.WithExterns(organizedExternAliasList).WithUsings(organizedUsingList);
        }

        public override SyntaxNode VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            => VisitBaseNamespaceDeclaration((BaseNamespaceDeclarationSyntax?)base.VisitFileScopedNamespaceDeclaration(node));

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            => VisitBaseNamespaceDeclaration((BaseNamespaceDeclarationSyntax?)base.VisitNamespaceDeclaration(node));

        private BaseNamespaceDeclarationSyntax VisitBaseNamespaceDeclaration(BaseNamespaceDeclarationSyntax? node)
        {
            Contract.ThrowIfNull(node);
            UsingsAndExternAliasesOrganizer.Organize(
                node.Externs, node.Usings,
                _placeSystemNamespaceFirst, _separateGroups,
                _fallbackTrivia,
                out var organizedExternAliasList, out var organizedUsingList);

            return node.WithExterns(organizedExternAliasList).WithUsings(organizedUsingList);
        }
    }
}
