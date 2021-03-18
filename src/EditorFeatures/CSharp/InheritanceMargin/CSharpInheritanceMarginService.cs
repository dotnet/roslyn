// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InheritanceMargin
{
    [ExportLanguageService(typeof(IInheritanceMarginService), LanguageNames.CSharp), Shared]
    internal class CSharpInheritanceMarginService : AbstractInheritanceMarginService
    {
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public CSharpInheritanceMarginService()
        {
        }

        protected override ImmutableArray<SyntaxNode> GetMembers(SyntaxNode root, TextSpan spanToSearch)
        {
            var typeDeclarationNodes = root
                .DescendantNodes(node => node is TypeDeclarationSyntax && node.Span.IntersectsWith(spanToSearch));

            using var _ = PooledObjects.ArrayBuilder<SyntaxNode>.GetInstance(out var builder);
            foreach (var typeDeclarationNode in typeDeclarationNodes)
            {
                // 1. Add the type declaration node.(e.g. class, struct etc..)
                // Use its identifier's position as the line number, since we want the margin to be placed with the identifier
                builder.Add(typeDeclarationNode);

                // 2. Add type members inside this type declaration.
                foreach (var member in ((TypeDeclarationSyntax)typeDeclarationNode).Members)
                {
                    if (member.Span.IntersectsWith(spanToSearch))
                    {
                        if (member.IsKind(
                            SyntaxKind.MethodDeclaration,
                            SyntaxKind.PropertyDeclaration,
                            SyntaxKind.EventDeclaration))
                        {
                            builder.Add(member);
                        }

                        // For multiple events that declared in the same EventFieldDeclaration,
                        // add all VariableDeclarators
                        if (member is EventFieldDeclarationSyntax eventFieldDeclarationNode)
                        {
                            builder.AddRange(eventFieldDeclarationNode.Declaration.Variables);
                        }
                    }
                }
            }

            return builder.ToImmutableArray();
        }

        protected override int GetIdentifierLineNumber(SourceText sourceText, SyntaxNode declarationNode)
        {
            var lines = sourceText.Lines;
            var identifierSpanStart = declarationNode switch
            {
                MethodDeclarationSyntax methodDeclarationNode => methodDeclarationNode.Identifier.SpanStart,
                PropertyDeclarationSyntax propertyDeclarationNode => propertyDeclarationNode.Identifier.SpanStart,
                EventDeclarationSyntax eventDeclarationNode => eventDeclarationNode.Identifier.SpanStart,
                VariableDeclaratorSyntax variableDeclaratorNode => variableDeclaratorNode.Identifier.SpanStart,
                TypeDeclarationSyntax baseTypeDeclarationNode => baseTypeDeclarationNode.Identifier.SpanStart,
                // Shouldn't reach here since the input declaration nodes are coming from GetMembers() method above
                _ => throw ExceptionUtilities.UnexpectedValue(declarationNode),
            };

            return lines.GetLineFromPosition(identifierSpanStart).LineNumber;
        }
    }
}
