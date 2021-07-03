// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    [ExportLanguageService(typeof(IMembersPullerService), LanguageNames.CSharp), Shared]
    internal class CSharpMembersPullerService : AbstractMembersPullerService<UsingDirectiveSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMembersPullerService()
        {
        }

        protected override SyntaxNode EnsureLeadingBlankLineBeforeFirstMember(SyntaxNode node)
        {
            var members = node switch
            {
                CompilationUnitSyntax compilationUnit => compilationUnit.Members,
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Members,
                _ => throw ExceptionUtilities.UnexpectedValue(node)
            };
            if (members.Count == 0)
            {
                return node;
            }

            var firstMember = members.First();
            var firstMemberTrivia = firstMember.GetLeadingTrivia();

            // If the first member already contains a leading new line then, this will already break up the usings from these members.
            if (firstMemberTrivia.Count > 0 && firstMemberTrivia.First().IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return node;
            }

            var newFirstMember = firstMember.WithLeadingTrivia(firstMemberTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed));
            return node.ReplaceNode(firstMember, newFirstMember);
        }

        protected override async Task<IEnumerable<UsingDirectiveSyntax>> GetImportsAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root.DescendantNodesAndSelf()
                .Where((node) => node is CompilationUnitSyntax || node is NamespaceDeclarationSyntax)
                .SelectMany(node => node switch
                {
                    CompilationUnitSyntax c => c.Usings,
                    NamespaceDeclarationSyntax n => n.Usings,
                    _ => default,
                })
                .Distinct()
                .Select(import => import.NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
        }

        protected override bool IsValidUnnecessaryImport(UsingDirectiveSyntax import, IEnumerable<UsingDirectiveSyntax> list)
        {
            var name = import.Name.ToString();
            return list.Any(node => node.Name.ToString() == name);
        }
    }
}
