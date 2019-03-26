// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class TypeImportCompletionProvider : AbstractTypeImportCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        protected override HashSet<INamespaceSymbol> GetNamespacesInScope(SemanticModel semanticModel, SyntaxNode location, CancellationToken cancellationToken)
        {
            var result = new HashSet<INamespaceSymbol>(semanticModel.GetUsingNamespacesInScope(location));

            var containingNamespaceDeclaration = location.Ancestors().First(n => n.IsKind(SyntaxKind.NamespaceDeclaration) || n.IsKind(SyntaxKind.CompilationUnit));

            var symbol = semanticModel.GetDeclaredSymbol(containingNamespaceDeclaration, cancellationToken);

            if (symbol is INamespaceSymbol namespaceSymbol)
            {
                while (!namespaceSymbol.IsGlobalNamespace)
                {
                    result.Add(namespaceSymbol);
                    namespaceSymbol = namespaceSymbol.ContainingNamespace;
                }
            }

            return result;
        }

        protected override async Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            bool? result = await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(false);
            if (result.HasValue)
            {
                return result.Value;
            }

            return true;
        }

        private async Task<bool?> IsTriggerOnDotAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text[characterPosition] != '.')
            {
                return null;
            }

            // don't want to trigger after a number.  All other cases after dot are ok.
            var tree = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindToken(characterPosition);
            if (token.Kind() == SyntaxKind.DotToken)
            {
                token = token.GetPreviousToken();
            }

            return token.Kind() != SyntaxKind.NumericLiteralToken;
        }

        protected override async Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
        }
    }
}
