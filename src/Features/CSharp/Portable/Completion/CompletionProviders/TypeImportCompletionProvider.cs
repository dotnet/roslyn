// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class TypeImportCompletionProvider : AbstractTypeImportCompletionProvider
    {
        public TypeImportCompletionProvider(Workspace workspace)
            : base(workspace)
        { }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        protected override ImmutableHashSet<string> GetNamespacesInScope(SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Names in C# are case-sensitive
            var builder = ImmutableHashSet.CreateBuilder<string>();

            // Get namespaces from usings
            foreach (var usingInScope in semanticModel.GetUsingNamespacesInScope(location))
            {
                builder.Add(usingInScope.ToDisplayString(SymbolDisplayFormats.NameFormat));
            }

            // Get namespaces from containing namespace declaration.
            var containingNamespaceDeclaration = location.Ancestors()
                .First(n => n.IsKind(SyntaxKind.NamespaceDeclaration) || n.IsKind(SyntaxKind.CompilationUnit));
            var symbol = semanticModel.GetDeclaredSymbol(containingNamespaceDeclaration, cancellationToken);
            if (symbol is INamespaceSymbol namespaceSymbol)
            {
                while (namespaceSymbol != null)
                {
                    builder.Add(namespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat));
                    namespaceSymbol = namespaceSymbol.ContainingNamespace;
                }
            }

            return builder.ToImmutable();
        }

        protected override async Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // Need regular semantic model because we will use it to get imported namepsace symbols.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
        }
    }
}
