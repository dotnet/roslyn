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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.AliasAmbiguousType
{
    internal abstract class AbstractAliasAmbiguousTypeCodeFixProvider : CodeFixProvider
    {
        protected abstract string GetTextPreviewOfChange(string aliasName, ITypeSymbol typeSymbol);

        public override FixAllProvider GetFixAllProvider() => null;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Innermost: We are looking for an IdentifierName. IdentifierName is sometimes at the same span as its parent (e.g. SimpleBaseTypeSyntax).
            var diagnosticNode = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (!syntaxFacts.IsIdentifierName(diagnosticNode))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolInfo = semanticModel.GetSymbolInfo(diagnosticNode, cancellationToken);
            if (SymbolCandidatesContainsSupportedSymbols(symbolInfo))
            {
                var addImportService = document.GetLanguageService<IAddImportsService>();
                var syntaxGenerator = document.GetLanguageService<SyntaxGenerator>();
                var compilation = semanticModel.Compilation;
                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
                var codeActionsBuilder = ImmutableArray.CreateBuilder<CodeAction>(symbolInfo.CandidateSymbols.Length);
                foreach (var symbol in symbolInfo.CandidateSymbols.Cast<ITypeSymbol>())
                {
                    var typeName = symbol.Name;
                    var codeActionPreviewText = GetTextPreviewOfChange(typeName, symbol);
                    codeActionsBuilder.Add(new MyCodeAction(codeActionPreviewText, c =>
                        {
                            var aliasDirective = syntaxGenerator.AliasImportDeclaration(typeName, symbol);
                            var newRoot = addImportService.AddImport(compilation, root, diagnosticNode, aliasDirective, placeSystemNamespaceFirst);
                            return Task.FromResult(document.WithSyntaxRoot(newRoot));
                        }));
                }
                var groupingTitle = string.Format(FeaturesResources.Alias_ambiguous_type_0, diagnosticNode.ToString());
                var groupingCodeAction = new CodeActionWithNestedActions(groupingTitle, codeActionsBuilder.ToImmutable(), isInlinable: true);
                context.RegisterCodeFix(groupingCodeAction, context.Diagnostics.First());
            }
        }

        private static bool SymbolCandidatesContainsSupportedSymbols(SymbolInfo symbolInfo)
            => symbolInfo.CandidateReason == CandidateReason.Ambiguous &&
               // Arity: Aliases can only name closed constructed types. (See also proposal https://github.com/dotnet/csharplang/issues/1239)
               // Aliasing as a closed constructed type is possible but would require to remove the type arguments from the diagnosed node.
               // It is unlikely that the user wants that and so generic types are not supported.
               symbolInfo.CandidateSymbols.All(symbol => symbol.IsKind(SymbolKind.NamedType) &&
                                                         symbol.GetArity() == 0);

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }
    }
}
