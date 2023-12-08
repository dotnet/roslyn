// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ForEachCast
{
    internal abstract class AbstractForEachCastCodeFixProvider<TForEachStatementSyntax> : SyntaxEditorBasedCodeFixProvider
        where TForEachStatementSyntax : SyntaxNode
    {
        protected abstract ITypeSymbol GetForEachElementType(SemanticModel semanticModel, TForEachStatementSyntax forEachStatement);

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.ForEachCastDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Diagnostics.First().Properties.ContainsKey(ForEachCastHelpers.IsFixable))
            {
                RegisterCodeFix(context, AnalyzersResources.Add_explicit_cast, nameof(AbstractForEachCastCodeFixProvider<SyntaxNode>));
            }

            return Task.CompletedTask;
        }

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => diagnostic.Properties.ContainsKey(ForEachCastHelpers.IsFixable);

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (node is TForEachStatementSyntax foreachStatement)
                    AddCast(syntaxFacts, semanticModel, editor, foreachStatement, cancellationToken);
            }
        }

        public void AddCast(
            ISyntaxFacts syntaxFacts,
            SemanticModel semanticModel,
            SyntaxEditor editor,
            TForEachStatementSyntax forEachStatement,
            CancellationToken cancellationToken)
        {
            var expression = syntaxFacts.GetExpressionOfForeachStatement(forEachStatement);
            var loopOperation = (IForEachLoopOperation)semanticModel.GetRequiredOperation(forEachStatement, cancellationToken);
            var variableDeclarator = (IVariableDeclaratorOperation)loopOperation.LoopControlVariable;
            var enumerableType = semanticModel.Compilation.GetBestTypeByMetadataName(typeof(Enumerable).FullName!);

            // These were already verified to be non-null in the analyzer.
            Contract.ThrowIfNull(variableDeclarator.Symbol.Type);
            Contract.ThrowIfNull(enumerableType);

            var elementType = GetForEachElementType(semanticModel, forEachStatement);
            var conversion = semanticModel.Compilation.ClassifyCommonConversion(elementType, variableDeclarator.Symbol.Type);

            var rewritten = GetRewrittenCollection(editor.Generator, expression, variableDeclarator.Symbol.Type, conversion);

            // Add an annotation for System.Linq.Enumerable so that we add a `using System.Linq;` if not present.
            rewritten = rewritten.WithAdditionalAnnotations(
                Simplifier.Annotation, Simplifier.AddImportsAnnotation, SymbolAnnotation.Create(enumerableType));

            editor.ReplaceNode(expression, rewritten);
        }

        private SyntaxNode GetRewrittenCollection(
            SyntaxGenerator generator,
            SyntaxNode collection,
            ITypeSymbol iterationVariableType,
            CommonConversion conversion)
        {
            if (conversion.Exists && conversion.IsReference)
            {
                // for a reference cast we can insert `.Cast<DestType>()`
                return generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        collection,
                        generator.GenericName(
                            nameof(Enumerable.Cast),
                            new[] { iterationVariableType })));
            }
            else
            {
                // otherwise we need to ensure a language specific conversion by emitting the conversion into the code
                // like so: `.Select(v => (DestType)v)`
                return generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        collection,
                        generator.IdentifierName(nameof(Enumerable.Select))),
                    generator.ValueReturningLambdaExpression(
                        "v",
                        generator.ConvertExpression(iterationVariableType, generator.IdentifierName("v"))));
            }
        }
    }
}
