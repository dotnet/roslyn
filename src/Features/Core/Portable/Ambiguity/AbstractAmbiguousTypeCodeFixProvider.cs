// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.AmbiguityCodeFixProvider
{
    internal abstract class AbstractAmbiguousTypeCodeFixProvider : CodeFixProvider
    {
        protected abstract SyntaxNode GetAliasDirective(string typeName, ISymbol symbol);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var span = context.Span;
            var diagnostic = context.Diagnostics.First();
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var addImportService = document.GetLanguageService<IAddImportsService>();
            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticNode = root.FindNode(span);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolInfo = semanticModel.GetSymbolInfo(diagnosticNode, cancellationToken);
            if (SymbolInfoContainesSupportedSymbols(symbolInfo))
            {
                var codeActionsBuilder = ImmutableArray.CreateBuilder<CodeAction>(symbolInfo.CandidateSymbols.Length);
                var typeName = GetAliasFromDiagnsoticNode(syntaxFacts, diagnosticNode);
                foreach (var symbol in symbolInfo.CandidateSymbols)
                {
                    var aliasDirective = GetAliasDirective(typeName, symbol);
                    var newRoot = addImportService.AddImport(semanticModel.Compilation, root, diagnosticNode, aliasDirective, placeSystemNamespaceFirst);
                    var codeActionPreviewText = GetTextPreviewOfChange(aliasDirective, document.Project.Solution.Workspace);
                    codeActionsBuilder.Add(new MyCodeAction(codeActionPreviewText,
                                                            c => Task.FromResult(document.WithSyntaxRoot(newRoot))));
                }
                var groupedCodeAction = new GroupingCodeAction("Test", codeActionsBuilder.ToImmutable());
                context.RegisterCodeFix(groupedCodeAction, diagnostic);
            }
        }

        private static string GetTextPreviewOfChange(SyntaxNode newNode, Workspace workspace)
            => Formatter.Format(newNode, workspace).ToFullString();

        private static string GetAliasFromDiagnsoticNode(ISyntaxFactsService syntaxFacts, SyntaxNode diagnosticNode)
        {
            // The content of the node is a good candidate for the alias
            // For attributes VB requires that the alias ends with 'Attribute' while C# is fine with or without the suffix.
            var nodeText = diagnosticNode.ToString();
            if (syntaxFacts.IsAttribute(diagnosticNode) || syntaxFacts.IsAttribute(diagnosticNode.Parent))
            {
                if (!nodeText.EndsWith("Attribute"))
                {
                    nodeText += "Attribute";
                }
            }

            return nodeText;
        }

        private bool SymbolInfoContainesSupportedSymbols(SymbolInfo symbolInfo)
            => symbolInfo.CandidateReason == CandidateReason.Ambiguous &&
               // Arity: Aliases can not name unbound generic types. Only closed constructed types can be aliased.
               // Aliasing as a closed constructed type is possible but would require to remove the type arguments from the diagnosed node.
               // It is unlikely that the user wants that and so generic types are not supported.
               // SymbolKind.NamedType: only types can be aliased by this fix.
               symbolInfo.CandidateSymbols.All(symbol => symbol.IsKind(SymbolKind.NamedType) &&
                                                         symbol.GetArity() == 0);

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
