﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
    {
        private partial class State
        {
            public SemanticDocument Document { get; }
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

            public State(TService service, SemanticDocument document)
            {
                _service = service;
                this.Document = document;
            }

            public static State Generate(
                TService service,
                SemanticDocument document,
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                var state = new State(service, document);
                if (!state.TryInitialize(textSpan, cancellationToken))
                {
                    return null;
                }

                return state;
            }

            private bool TryInitialize(
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var tree = this.Document.SyntaxTree;
                var syntaxFacts = this.Document.Project.LanguageServices.GetService<ISyntaxFactsService>();

                this.Expression = this.GetExpressionUnderSpan(tree, textSpan, cancellationToken);
                if (this.Expression == null)
                {
                    return false;
                }

                var expressionType = this.Document.SemanticModel.GetTypeInfo(this.Expression, cancellationToken).Type;
                if (expressionType is IErrorTypeSymbol)
                {
                    return false;
                }

                var containingType = this.Expression.AncestorsAndSelf()
                    .Select(n => this.Document.SemanticModel.GetDeclaredSymbol(n, cancellationToken))
                    .OfType<INamedTypeSymbol>()
                    .FirstOrDefault();

                containingType = containingType ?? this.Document.SemanticModel.Compilation.ScriptClass;

                if (containingType == null || containingType.TypeKind == TypeKind.Interface)
                {
                    return false;
                }

                if (!CanIntroduceVariable(textSpan.IsEmpty, cancellationToken))
                {
                    return false;
                }

                this.IsConstant = this.Document.SemanticModel.GetConstantValue(this.Expression, cancellationToken).HasValue;

                // Note: the ordering of these clauses are important.  They go, generally, from 
                // innermost to outermost order.  
                if (IsInQueryContext(cancellationToken))
                {
                    if (CanGenerateInto<TQueryExpressionSyntax>(cancellationToken))
                    {
                        this.InQueryContext = true;
                        return true;
                    }

                    return false;
                }

                if (IsInConstructorInitializerContext(cancellationToken))
                {
                    if (CanGenerateInto<TTypeDeclarationSyntax>(cancellationToken))
                    {
                        this.InConstructorInitializerContext = true;
                        return true;
                    }

                    return false;
                }

                var enclosingBlocks = _service.GetContainingExecutableBlocks(this.Expression);
                if (enclosingBlocks.Any())
                {
                    // If we're inside a block, then don't even try the other options (like field,
                    // constructor initializer, etc.).  This is desirable behavior.  If we're in a 
                    // block in a field, then we're in a lambda, and we want to offer to generate
                    // a local, and not a field.
                    if (IsInBlockContext(cancellationToken))
                    {
                        this.InBlockContext = true;
                        return true;
                    }

                    return false;
                }

                /* NOTE: All checks from this point forward are intentionally ordered to be AFTER the check for Block Context. */

                // If we are inside a block within an Expression bodied member we should generate inside the block, 
                // instead of rewriting a concise expression bodied member to its equivalent that has a body with a block.
                if (_service.IsInExpressionBodiedMember(this.Expression))
                {
                    if (CanGenerateInto<TTypeDeclarationSyntax>(cancellationToken))
                    {
                        this.InExpressionBodiedMemberContext = true;
                        return true;
                    }

                    return false;
                }

                if (_service.IsInAutoPropertyInitializer(this.Expression))
                {
                    if (CanGenerateInto<TTypeDeclarationSyntax>(cancellationToken))
                    {
                        this.InAutoPropertyInitializerContext = true;
                        return true;
                    }

                    return false;
                }

                if (CanGenerateInto<TTypeDeclarationSyntax>(cancellationToken))
                {
                    if (IsInParameterContext(cancellationToken))
                    {
                        this.InParameterContext = true;
                        return true;
                    }
                    else if (IsInFieldContext(cancellationToken))
                    {
                        this.InFieldContext = true;
                        return true;
                    }
                    else if (IsInAttributeContext(cancellationToken))
                    {
                        this.InAttributeContext = true;
                        return true;
                    }
                }

                return false;
            }

            public SemanticMap GetSemanticMap(CancellationToken cancellationToken)
            {
                _semanticMap = _semanticMap ?? this.Document.SemanticModel.GetSemanticMap(this.Expression, cancellationToken);
                return _semanticMap;
            }

            private TExpressionSyntax GetExpressionUnderSpan(SyntaxTree tree, TextSpan textSpan, CancellationToken cancellationToken)
            {
                var root = tree.GetRoot(cancellationToken);

                // If there is no selection, pick the 'best' expression we're currently touching.
                if (textSpan.Length == 0)
                {
                    return GetBestTouchingExpression(root, textSpan.Start);
                }

                var startToken = root.FindToken(textSpan.Start);
                var syntaxFacts = this.Document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                if (startToken.Span.End < textSpan.Start && startToken.TrailingTrivia.All(t => syntaxFacts.IsWhitespaceOrEndOfLineTrivia(t)))
                {
                    // We are pointing in the trailing whitespace trivia of a token, we shouldn't include that token
                    startToken = startToken.GetNextToken();
                    var newStart = startToken.Span.Start;

                    if (textSpan.End < newStart)
                    {
                        // Additional trivia exists between 'textSpan' and the start of the next token, so the two are not considered adjacent
                        return null;
                    }

                    textSpan = TextSpan.FromBounds(newStart, textSpan.End);
                }

                var stopToken = root.FindToken(textSpan.End);

                if (textSpan.End <= stopToken.SpanStart)
                {
                    stopToken = stopToken.GetPreviousToken(includeSkipped: true);
                }

                if (startToken.RawKind == 0 || stopToken.RawKind == 0)
                {
                    return null;
                }

                var containingExpressions1 = startToken.GetAncestors<TExpressionSyntax>().ToList();
                var containingExpressions2 = stopToken.GetAncestors<TExpressionSyntax>().ToList();

                var commonExpression = containingExpressions1.FirstOrDefault(containingExpressions2.Contains);
                if (commonExpression == null)
                {
                    return null;
                }

                if (!(textSpan.Start >= commonExpression.FullSpan.Start &&
                      textSpan.Start <= commonExpression.SpanStart))
                {
                    return null;
                }

                if (!(textSpan.End >= commonExpression.Span.End &&
                      textSpan.End <= commonExpression.FullSpan.End))
                {
                    return null;
                }

                return commonExpression;
            }

            private static TExpressionSyntax GetBestTouchingExpression(SyntaxNode root, int position)
            {
                var exprOnRight = root.FindToken(position).Parent as TExpressionSyntax;
                var exprOnLeft = position > 0
                    ? root.FindToken(position - 1).Parent as TExpressionSyntax
                    : null;

                // Only get the expr on the left if we're right at the end of it.
                if (exprOnLeft?.Span.End != position)
                {
                    return exprOnRight;
                }

                // If we have two non-overlapping expressions, then just pick the expression
                // to the right of the caret.  However, if they overlap, pick the smaller of
                // the two as the user likely thinks that ones is more closely associated 
                // with the caret.
                return exprOnRight == null || exprOnRight.Span.Contains(exprOnLeft.Span)
                    ? exprOnLeft
                    : exprOnRight;
            }

            private bool CanIntroduceVariable(
                bool isSpanEmpty,
                CancellationToken cancellationToken)
            {
                if (!_service.CanIntroduceVariableFor(this.Expression))
                {
                    return false;
                }

                if (isSpanEmpty && this.Expression is TNameSyntax)
                {
                    // to extract a name, you must have a selection (this avoids making the refactoring too noisy)
                    return false;
                }

                if (this.Expression is TTypeSyntax && !(this.Expression is TNameSyntax))
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
                var semanticFacts = this.Document.Project.LanguageServices.GetService<ISemanticFactsService>();
                return semanticFacts.CanReplaceWithRValue(this.Document.SemanticModel, this.Expression, cancellationToken);
            }

            private bool CanGenerateInto<TSyntax>(CancellationToken cancellationToken)
                where TSyntax : SyntaxNode
            {
                if (this.Document.SemanticModel.Compilation.ScriptClass != null)
                {
                    return true;
                }

                var syntax = this.Expression.GetAncestor<TSyntax>();
                return syntax != null && !syntax.OverlapsHiddenPosition(cancellationToken);
            }

            private bool IsInTypeDeclarationOrValidCompilationUnit()
            {
                if (this.Expression.GetAncestorOrThis<TTypeDeclarationSyntax>() != null)
                {
                    return true;
                }

                // If we're interactive/script, we can generate into the compilation unit.
                if (this.Document.Document.SourceCodeKind != SourceCodeKind.Regular)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
