// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
    {
        private sealed partial class State
        {
            public SemanticDocument Document { get; }
            public CodeCleanupOptions Options { get; }
            public TExpressionSyntax Expression { get; private set; }

            public bool InAttributeContext { get; private set; }
            public bool InBlockContext { get; private set; }
            public bool InConstructorInitializerContext { get; private set; }
            public bool InFieldContext { get; private set; }
            public bool InParameterContext { get; private set; }
            public bool InQueryContext { get; private set; }
            public bool InExpressionBodiedMemberContext { get; private set; }
            public bool InAutoPropertyInitializerContext { get; private set; }

            public bool IsConstant { get; private set; }

            private SemanticMap _semanticMap;
            private readonly TService _service;

            public State(TService service, SemanticDocument document, CodeCleanupOptions options)
            {
                _service = service;
                Document = document;
                Options = options;
            }

            public static async Task<State> GenerateAsync(
                TService service,
                SemanticDocument document,
                CodeCleanupOptions options,
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                var state = new State(service, document, options);
                if (!await state.TryInitializeAsync(document, textSpan, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                SemanticDocument document,
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Expression = await document.Document.TryGetRelevantNodeAsync<TExpressionSyntax>(textSpan, cancellationToken).ConfigureAwait(false);
                if (Expression == null || CodeRefactoringHelpers.IsNodeUnderselected(Expression, textSpan))
                    return false;

                // Don't introduce constant for another constant. Doesn't apply to sub-expression of constant.
                if (IsInitializerOfConstant(document, Expression))
                    return false;

                var expressionType = Document.SemanticModel.GetTypeInfo(Expression, cancellationToken).Type;
                if (expressionType is IErrorTypeSymbol)
                    return false;

                var containingType = Expression.AncestorsAndSelf()
                    .Select(n => Document.SemanticModel.GetDeclaredSymbol(n, cancellationToken))
                    .OfType<INamedTypeSymbol>()
                    .FirstOrDefault();

                containingType ??= Document.SemanticModel.Compilation.ScriptClass;

                if (containingType == null || containingType.TypeKind == TypeKind.Interface)
                    return false;

                if (!CanIntroduceVariable(textSpan.IsEmpty, cancellationToken))
                    return false;

                IsConstant = IsExpressionConstant(Document, Expression, _service, cancellationToken);

                // Note: the ordering of these clauses are important.  They go, generally, from
                // innermost to outermost order.
                if (IsInQueryContext(cancellationToken))
                {
                    if (CanGenerateInto<TQueryExpressionSyntax>(cancellationToken))
                    {
                        InQueryContext = true;
                        return true;
                    }

                    return false;
                }

                if (IsInConstructorInitializerContext(cancellationToken))
                {
                    if (CanGenerateInto<TTypeDeclarationSyntax>(cancellationToken))
                    {
                        InConstructorInitializerContext = true;
                        return true;
                    }

                    return false;
                }

                var enclosingBlocks = _service.GetContainingExecutableBlocks(Expression);
                if (enclosingBlocks.Any())
                {
                    // If we're inside a block, then don't even try the other options (like field,
                    // constructor initializer, etc.).  This is desirable behavior.  If we're in a 
                    // block in a field, then we're in a lambda, and we want to offer to generate
                    // a local, and not a field.
                    if (IsInBlockContext(cancellationToken))
                    {
                        InBlockContext = true;
                        return true;
                    }

                    return false;
                }

                // NOTE: All checks from this point forward are intentionally ordered to be AFTER the check for Block Context.

                // If we are inside a block within an Expression bodied member we should generate inside the block, 
                // instead of rewriting a concise expression bodied member to its equivalent that has a body with a block.
                if (_service.IsInExpressionBodiedMember(Expression))
                {
                    if (CanGenerateInto<TTypeDeclarationSyntax>(cancellationToken))
                    {
                        InExpressionBodiedMemberContext = true;
                        return true;
                    }

                    return false;
                }

                if (_service.IsInAutoPropertyInitializer(Expression))
                {
                    if (CanGenerateInto<TTypeDeclarationSyntax>(cancellationToken))
                    {
                        InAutoPropertyInitializerContext = true;
                        return true;
                    }

                    return false;
                }

                if (CanGenerateInto<TTypeDeclarationSyntax>(cancellationToken))
                {
                    if (IsInParameterContext(cancellationToken))
                    {
                        InParameterContext = true;
                        return true;
                    }
                    else if (IsInFieldContext(cancellationToken))
                    {
                        InFieldContext = true;
                        return true;
                    }
                    else if (IsInAttributeContext())
                    {
                        InAttributeContext = true;
                        return true;
                    }
                }

                return false;

                static bool IsInitializerOfConstant(SemanticDocument document, TExpressionSyntax expression)
                {
                    var syntaxFacts = document.Document.GetRequiredLanguageService<ISyntaxFactsService>();

                    var current = expression;
                    while (syntaxFacts.IsParenthesizedExpression(current.Parent))
                        current = (TExpressionSyntax)current.Parent;

                    if (!syntaxFacts.IsEqualsValueClause(current.Parent))
                        return false;

                    var equalsValue = current.Parent;
                    if (!syntaxFacts.IsVariableDeclarator(equalsValue.Parent))
                        return false;

                    var declaration = equalsValue.AncestorsAndSelf().FirstOrDefault(n => syntaxFacts.IsLocalDeclarationStatement(n) || syntaxFacts.IsFieldDeclaration(n));
                    if (declaration == null)
                        return false;

                    var generator = SyntaxGenerator.GetGenerator(document.Document);
                    return generator.GetModifiers(declaration).IsConst;
                }

                static bool IsExpressionConstant(SemanticDocument document, TExpressionSyntax expression, TService service, CancellationToken cancellationToken)
                {
                    if (document.SemanticModel.GetConstantValue(expression, cancellationToken) is { HasValue: true, Value: var value })
                    {
                        var syntaxKindsService = document.Document.GetRequiredLanguageService<ISyntaxKindsService>();
                        if (syntaxKindsService.InterpolatedStringExpression == expression.RawKind && value is string)
                        {
                            // Interpolated strings can have constant values, but if it's being converted to a FormattableString
                            // or IFormattable then we cannot treat it as one
                            var typeInfo = document.SemanticModel.GetTypeInfo(expression, cancellationToken);
                            return typeInfo.ConvertedType?.IsFormattableStringOrIFormattable() != true;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public SemanticMap GetSemanticMap(CancellationToken cancellationToken)
            {
                _semanticMap ??= Document.SemanticModel.GetSemanticMap(Expression, cancellationToken);
                return _semanticMap;
            }

            private bool CanIntroduceVariable(
                bool isSpanEmpty,
                CancellationToken cancellationToken)
            {
                if (!_service.CanIntroduceVariableFor(Expression))
                {
                    return false;
                }

                if (isSpanEmpty && Expression is TNameSyntax)
                {
                    // to extract a name, you must have a selection (this avoids making the refactoring too noisy)
                    return false;
                }

                if (Expression is TTypeSyntax and not TNameSyntax)
                {
                    // name syntax can introduce variables, but not other type syntaxes
                    return false;
                }

                // Even though we're creating a variable, we still ask if we can be replaced with an
                // RValue and not an LValue.  This is because introduction of a local adds a *new* LValue
                // location, and we want to ensure that any writes will still happen to the *original*
                // LValue location.  i.e. if you have: "a[1] = b" then you don't want to change that to
                // "var c = a[1]; c = b", as that write is no longer happening into the right LValue.
                //
                // In essence, this says "i can be replaced with an expression as long as I'm not being
                // written to".
                var semanticFacts = Document.Project.LanguageServices.GetService<ISemanticFactsService>();
                return semanticFacts.CanReplaceWithRValue(Document.SemanticModel, Expression, cancellationToken);
            }

            private bool CanGenerateInto<TSyntax>(CancellationToken cancellationToken)
                where TSyntax : SyntaxNode
            {
                if (Document.SemanticModel.Compilation.ScriptClass != null)
                {
                    return true;
                }

                var syntax = Expression.GetAncestor<TSyntax>();
                return syntax != null && !syntax.OverlapsHiddenPosition(cancellationToken);
            }

            private bool IsInTypeDeclarationOrValidCompilationUnit()
            {
                if (Expression.GetAncestorOrThis<TTypeDeclarationSyntax>() != null)
                {
                    return true;
                }

                // If we're interactive/script, we can generate into the compilation unit.
                if (Document.Document.SourceCodeKind != SourceCodeKind.Regular)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
