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

        protected override SyntaxTriviaList GetFirstMemberTrivia(SyntaxNode root)
        {
            var members = root switch
            {
                CompilationUnitSyntax compilationUnit => compilationUnit.Members,
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Members,
                _ => throw ExceptionUtilities.UnexpectedValue(root)
            };

            if (members.Count == 0)
            {
                return SyntaxTriviaList.Empty;
            }

            return members.First().GetLeadingTrivia();
        }

        protected override SyntaxNode EnsureLeadingTriviaBeforeFirstMember(SyntaxNode root, SyntaxTriviaList trivia)
        {
            var members = root switch
            {
                CompilationUnitSyntax compilationUnit => compilationUnit.Members,
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Members,
                _ => throw ExceptionUtilities.UnexpectedValue(root)
            };

            if (members.Count == 0)
            {
                return root;
            }

            var firstMember = members.First();
            return root.ReplaceNode(firstMember, firstMember.WithLeadingTrivia(trivia));
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
