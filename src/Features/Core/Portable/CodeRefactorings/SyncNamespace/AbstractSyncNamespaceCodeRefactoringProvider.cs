// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax> :
        CodeRefactoringProvider
        where TService : AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode 
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;            

            var state = await State.CreateAsync((TService)this, document, textSpan, cancellationToken);
            if (state == null)
            {
                return;
            }

            context.RegisterRefactorings(SyncNamespaceCodeAction.CreateCodeActions((TService)this, state));
        }

        private static INamespaceSymbol GetDeclaredNamespaceSymbol(TNamespaceDeclarationSyntax node, SemanticModel semanticModel)
        {
            return semanticModel.GetDeclaredSymbol(node) as INamespaceSymbol;
        }                       

        abstract protected TNamespaceDeclarationSyntax ChangeNamespace(TNamespaceDeclarationSyntax node, ImmutableArray<string> namespaceParts);

        abstract protected bool TryGetReplacementSyntax(SyntaxNode reference, ImmutableArray<string> namespaceParts, out SyntaxNode old, out SyntaxNode @new);

        abstract protected ImmutableArray<ISymbol> GetDeclaredSymbols(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken);

        abstract protected SyntaxNode CreateUsingDirective(ImmutableArray<string> namespaceParts);

        private static bool IsProperPrefix(string @namespace, string text, out string qualifiedIdentifierSuffix)
        {
            Debug.Assert(@namespace != null && text != null);

            qualifiedIdentifierSuffix = null;
            if (string.Equals(@namespace, text, StringComparison.Ordinal))
            {
                qualifiedIdentifierSuffix = string.Empty;
            }
            else if (@namespace.StartsWith(text + ".", StringComparison.Ordinal))
            {
                qualifiedIdentifierSuffix = @namespace.Substring(text.Length + 1);
            }
            else if (text.Length == 0)
            {
                qualifiedIdentifierSuffix = @namespace;
            }

            return qualifiedIdentifierSuffix == null;
        }
    }
}
