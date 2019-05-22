// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SyncNamespace), Shared]
    internal sealed class CSharpSyncNamespaceCodeRefactoringProvider
        : AbstractSyncNamespaceCodeRefactoringProvider<NamespaceDeclarationSyntax, CompilationUnitSyntax, MemberDeclarationSyntax>
    {
        [ImportingConstructor]
        public CSharpSyncNamespaceCodeRefactoringProvider()
        {
        }

        protected override async Task<SyntaxNode> TryGetApplicableInvocationNodeAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            if (!span.IsEmpty)
            {
                return null;
            }

            var position = span.Start;

            var compilationUnit = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var namespaceDecls = compilationUnit.DescendantNodes(n => n is CompilationUnitSyntax || n is NamespaceDeclarationSyntax)
                .OfType<NamespaceDeclarationSyntax>().ToImmutableArray();

            if (namespaceDecls.Length == 1 && compilationUnit.Members.Count == 1)
            {
                var namespaceDeclaration = namespaceDecls[0];

                if (namespaceDeclaration.Name.Span.IntersectsWith(position))
                {
                    return namespaceDeclaration;
                }
            }

            if (namespaceDecls.Length == 0)
            {
                var firstMemberDeclarationName = compilationUnit.Members.FirstOrDefault().GetNameToken();

                if (firstMemberDeclarationName != default
                    && firstMemberDeclarationName.Span.IntersectsWith(position))
                {
                    return compilationUnit;
                }
            }

            return null;
        }

        protected override string EscapeIdentifier(string identifier)
            => identifier.EscapeIdentifier();
    }
}
