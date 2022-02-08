// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class MemberDeclarationSyntaxExtensions
    {
        private sealed class DeclarationFinder : CSharpSyntaxWalker
        {
            private readonly Dictionary<string, List<SyntaxToken>> _map = new();

            private DeclarationFinder()
                : base(SyntaxWalkerDepth.Node)
            {
            }

            public static Dictionary<string, List<SyntaxToken>> GetAllDeclarations(SyntaxNode syntax)
            {
                var finder = new DeclarationFinder();
                finder.Visit(syntax);
                return finder._map;
            }

            private void Add(SyntaxToken syntaxToken)
            {
                if (syntaxToken.Kind() == SyntaxKind.IdentifierToken)
                {
                    var identifier = syntaxToken.ValueText;
                    if (!_map.TryGetValue(identifier, out var list))
                    {
                        list = new List<SyntaxToken>();
                        _map.Add(identifier, list);
                    }

                    list.Add(syntaxToken);
                }
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                base.VisitVariableDeclarator(node);
                Add(node.Identifier);
            }

            public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
            {
                base.VisitCatchDeclaration(node);
                Add(node.Identifier);
            }

            public override void VisitParameter(ParameterSyntax node)
            {
                base.VisitParameter(node);
                Add(node.Identifier);
            }

            public override void VisitFromClause(FromClauseSyntax node)
            {
                base.VisitFromClause(node);
                Add(node.Identifier);
            }

            public override void VisitLetClause(LetClauseSyntax node)
            {
                base.VisitLetClause(node);
                Add(node.Identifier);
            }

            public override void VisitJoinClause(JoinClauseSyntax node)
            {
                base.VisitJoinClause(node);
                Add(node.Identifier);
            }

            public override void VisitJoinIntoClause(JoinIntoClauseSyntax node)
            {
                base.VisitJoinIntoClause(node);
                Add(node.Identifier);
            }

            public override void VisitQueryContinuation(QueryContinuationSyntax node)
            {
                base.VisitQueryContinuation(node);
                Add(node.Identifier);
            }
        }
    }
}
