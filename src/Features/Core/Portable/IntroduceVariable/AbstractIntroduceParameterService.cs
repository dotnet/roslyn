// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

#nullable disable

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract partial class AbstractIntroduceParameterService<TService, TExpressionSyntax> : CodeRefactoringProvider
        where TService : AbstractIntroduceParameterService<TService, TExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
    {
        public TExpressionSyntax Expression { get; private set; }
        protected abstract Task<Document> IntroduceParameterAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, CancellationToken cancellationToken);
        protected abstract IEnumerable<SyntaxNode> GetContainingExecutableBlocks(TExpressionSyntax expression);
        protected virtual bool BlockOverlapsHiddenPosition(SyntaxNode block, CancellationToken cancellationToken)
            => block.OverlapsHiddenPosition(cancellationToken);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var action = await IntroduceParameterAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (action != null)
            {
                context.RegisterRefactoring(action, textSpan);
            }
        }

        public async Task<CodeAction> IntroduceParameterAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            Expression = await document.TryGetRelevantNodeAsync<TExpressionSyntax>(textSpan, cancellationToken).ConfigureAwait(false);
            if (Expression == null || CodeRefactoringHelpers.IsNodeUnderselected(Expression, textSpan))
            {
                return null;
            }

            var expressionType = semanticDocument.SemanticModel.GetTypeInfo(Expression, cancellationToken).Type;
            if (expressionType is IErrorTypeSymbol)
            {
                return null;
            }

            var containingType = Expression.AncestorsAndSelf()
                .Select(n => semanticDocument.SemanticModel.GetDeclaredSymbol(n, cancellationToken))
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault();

            containingType ??= semanticDocument.SemanticModel.Compilation.ScriptClass;

            if (containingType == null || containingType.TypeKind == TypeKind.Interface)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (Expression != null)
            {
                var (title, actions) = AddActions(semanticDocument, cancellationToken);

                if (actions.Length > 0)
                {
                    return new CodeActionWithNestedActions(title, actions, isInlinable: true);
                }
            }
            return null;

        }

        private (string title, ImmutableArray<CodeAction> actions) AddActions(SemanticDocument semanticDocument, CancellationToken cancellationToken)
        {
            var actionsBuilder = new ArrayBuilder<CodeAction>();
            var enclosingBlocks = GetContainingExecutableBlocks(Expression);
            if (enclosingBlocks.Any())
            {
                // If we're inside a block, then don't even try the other options (like field,
                // constructor initializer, etc.).  This is desirable behavior.  If we're in a 
                // block in a field, then we're in a lambda, and we want to offer to generate
                // a local, and not a field.
                if (IsInBlockContext(semanticDocument, cancellationToken))
                {
                    var block = enclosingBlocks.FirstOrDefault();

                    if (!BlockOverlapsHiddenPosition(block, cancellationToken))
                    {
                        actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, Expression, false));

                        if (enclosingBlocks.All(b => !BlockOverlapsHiddenPosition(b, cancellationToken)))
                        {
                            actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, Expression, true));
                        }
                    }
                }
            }
            return (FeaturesResources.Introduce_parameter, actionsBuilder.ToImmutable());
        }

        protected static ITypeSymbol GetTypeSymbol(
            SemanticDocument document,
            TExpressionSyntax expression,
            CancellationToken cancellationToken,
            bool objectAsDefault = true)
        {
            var semanticModel = document.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

            if (typeInfo.Type?.SpecialType == SpecialType.System_String &&
                typeInfo.ConvertedType?.IsFormattableStringOrIFormattable() == true)
            {
                return typeInfo.ConvertedType;
            }

            if (typeInfo.Type != null)
            {
                return typeInfo.Type;
            }

            if (typeInfo.ConvertedType != null)
            {
                return typeInfo.ConvertedType;
            }

            if (objectAsDefault)
            {
                return semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            }

            return null;
        }

        private bool IsInBlockContext(SemanticDocument semanticDocument,
                CancellationToken cancellationToken)
        {
            /* if (!IsInTypeDeclarationOrValidCompilationUnit())
             {
                 return false;
             }*/

            // If refer to a query property, then we use the query context instead.
            var bindingMap = GetSemanticMap(semanticDocument, cancellationToken);
            if (bindingMap.AllReferencedSymbols.Any(s => s is IRangeVariableSymbol))
            {
                return false;
            }

            var type = GetTypeSymbol(semanticDocument, Expression, cancellationToken, objectAsDefault: false);
            if (type == null || type.SpecialType == SpecialType.System_Void)
            {
                return false;
            }

            return true;
        }

        public SemanticMap GetSemanticMap(SemanticDocument semanticDocument, CancellationToken cancellationToken)
        {
            var semanticMap = semanticDocument.SemanticModel.GetSemanticMap(Expression, cancellationToken);
            return semanticMap;
        }

        private class IntroduceParameterCodeAction : AbstractIntroduceParameterCodeAction
        {
            internal IntroduceParameterCodeAction(
                SemanticDocument document,
                TService service,
                TExpressionSyntax expression,
                bool allOccurrences)
                : base(document, service, expression, allOccurrences)
            { }
        }
    }
}
