// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
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

        protected override ImmutableArray<UsingDirectiveSyntax> GetImports(SyntaxNode node)
        {
            return node.AncestorsAndSelf()
                .Where(node => node is CompilationUnitSyntax || node is NamespaceDeclarationSyntax)
                .SelectMany(node => node switch
                {
                    CompilationUnitSyntax c => c.Usings,
                    NamespaceDeclarationSyntax n => n.Usings,
                    _ => throw ExceptionUtilities.UnexpectedValue(node),
                })
                .SelectAsArray(import => import
                    .NormalizeWhitespace()
                    .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
        }
    }
}
