// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.OrganizeImports
{
    internal partial class CSharpOrganizeImportsService
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly bool _placeSystemNamespaceFirst;
            private readonly bool _separateGroups;

            public readonly IList<TextChange> TextChanges = new List<TextChange>();

            public Rewriter(bool placeSystemNamespaceFirst,
                            bool separateGroups)
            {
                _placeSystemNamespaceFirst = placeSystemNamespaceFirst;
                _separateGroups = separateGroups;
            }

            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                node = (CompilationUnitSyntax)base.VisitCompilationUnit(node);
                UsingsAndExternAliasesOrganizer.Organize(
                    node.Externs, node.Usings,
                    _placeSystemNamespaceFirst, _separateGroups,
                    out var organizedExternAliasList, out var organizedUsingList);

                var result = node.WithExterns(organizedExternAliasList).WithUsings(organizedUsingList);
                if (node != result)
                {
                    AddTextChange(node.Externs, organizedExternAliasList);
                    AddTextChange(node.Usings, organizedUsingList);
                }

                return result;
            }

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                node = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node);
                UsingsAndExternAliasesOrganizer.Organize(
                    node.Externs, node.Usings,
                    _placeSystemNamespaceFirst, _separateGroups,
                    out var organizedExternAliasList, out var organizedUsingList);

                var result = node.WithExterns(organizedExternAliasList).WithUsings(organizedUsingList);
                if (node != result)
                {
                    AddTextChange(node.Externs, organizedExternAliasList);
                    AddTextChange(node.Usings, organizedUsingList);
                }

                return result;
            }

            private void AddTextChange<TSyntax>(SyntaxList<TSyntax> list, SyntaxList<TSyntax> organizedList)
                where TSyntax : SyntaxNode
            {
                if (list.Count > 0)
                {
                    this.TextChanges.Add(new TextChange(GetTextSpan(list), GetNewText(organizedList)));
                }
            }

            private string GetNewText<TSyntax>(SyntaxList<TSyntax> organizedList)
                where TSyntax : SyntaxNode
            {
                return string.Join(string.Empty, organizedList.Select(t => t.ToFullString()));
            }

            private TextSpan GetTextSpan<TSyntax>(SyntaxList<TSyntax> list)
                where TSyntax : SyntaxNode
            {
                return TextSpan.FromBounds(list.First().FullSpan.Start, list.Last().FullSpan.End);
            }
        }
    }
}
