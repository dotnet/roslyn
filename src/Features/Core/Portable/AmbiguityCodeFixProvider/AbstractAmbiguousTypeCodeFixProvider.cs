// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.AmbiguityCodeFixProvider
{
    internal abstract class AbstractAmbiguousTypeCodeFixProvider : CodeFixProvider
    {
        protected abstract SyntaxNode GetAliasDirective(string typeName, ISymbol symbol);

        protected abstract Task<Document> InsertAliasDirective(Document document, SyntaxNode nodeReferencingType, SyntaxNode aliasDirectiveToInsert, CancellationToken cancellationToken);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var diagnostic = diagnostics.First();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticNode = root.FindNode(span);
            var typeName = diagnosticNode.ToString();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolInfo = semanticModel.GetSymbolInfo(diagnosticNode, cancellationToken);
            if (SymbolInfoContainesSupportedSymbols(symbolInfo))
            {
                var codeActionsBuilder = ImmutableArray.CreateBuilder<CodeAction>(symbolInfo.CandidateSymbols.Length);
                foreach (var symbol in symbolInfo.CandidateSymbols)
                {
                    var aliasDirective = GetAliasDirective(typeName, symbol);
                    codeActionsBuilder.Add(new MyCodeAction(symbol.ContainingNamespace.Name,
                                                            c => InsertAliasDirective(document, diagnosticNode, aliasDirective, c)));
                }
                context.RegisterFixes(codeActionsBuilder.ToImmutable(), diagnostic);
            }
        }

        private bool SymbolInfoContainesSupportedSymbols(SymbolInfo symbolInfo)
            => symbolInfo.CandidateReason == CandidateReason.Ambiguous &&
               // Arity: Aliases can not name unbound generic types. Only closed constructed types can be aliased.
               // Aliasing as a closed constructed type is possible but would require to remove the type arguments from the diagnosed node.
               // It is unlikely that the user wants that and so generic types are not supported.
               // SymbolKind.Namespace: see test method TestAmbiguousAliasNoDiagnostics
               symbolInfo.CandidateSymbols.All(symbol => symbol.GetArity() == 0 &&
                                                         !symbol.IsKind(SymbolKind.Namespace)); 

        private static string GetNodeName(ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetNameAndArityOfSimpleName(node, out var name, out var arity);
            return name;
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }

        private class GroupingCodeAction : CodeActionWithNestedActions
        {
            public GroupingCodeAction(string title, ImmutableArray<CodeAction> nestedActions)
                : base(title, nestedActions, isInlinable: true)
            {
            }
        }
    }
}
