// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        protected override SyntaxList<SyntaxNode> GetMembers(SyntaxNode root)
        {
            return root switch
            {
                CompilationUnitSyntax compilationUnit => compilationUnit.Members,
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Members,
                _ => throw ExceptionUtilities.UnexpectedValue(root)
            };
        }

        protected override async Task<ImmutableArray<UsingDirectiveSyntax>> GetImportsAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root.DescendantNodesAndSelf()
                .Where(node => node is CompilationUnitSyntax || node is NamespaceDeclarationSyntax)
                .SelectMany(node => node switch
                {
                    CompilationUnitSyntax c => c.Usings,
                    NamespaceDeclarationSyntax n => n.Usings,
                    _ => throw ExceptionUtilities.UnexpectedValue(node),
                })
                .SelectAsArray(import => import.NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
        }

        protected override bool IsValidUnnecessaryImport(UsingDirectiveSyntax import, UsingDirectiveSyntax destImport)
            => SyntaxFactory.AreEquivalent(import, destImport);
    }
}
