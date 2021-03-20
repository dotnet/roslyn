﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    internal class CSharpRecommendationServiceRunner : AbstractRecommendationServiceRunner<CSharpSyntaxContext>
    {
        public CSharpRecommendationServiceRunner(
            CSharpSyntaxContext context, bool filterOutOfScopeLocals, CancellationToken cancellationToken)
            : base(context, filterOutOfScopeLocals, cancellationToken)
        {
        }

        public override ImmutableArray<ISymbol> GetSymbols()
        {
            if (_context.IsInNonUserCode ||
                _context.IsPreProcessorDirectiveContext)
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            return _context.IsRightOfNameSeparator
                ? GetSymbolsOffOfContainer()
                : GetSymbolsForCurrentContext();
        }

        public override bool TryGetExplicitTypeOfLambdaParameter(SyntaxNode lambdaSyntax, int ordinalInLambda, [NotNullWhen(true)] out ITypeSymbol? explicitLambdaParameterType)
        {
            if (lambdaSyntax.IsKind<ParenthesizedLambdaExpressionSyntax>(SyntaxKind.ParenthesizedLambdaExpression, out var parenthesizedLambdaSyntax))
            {
                var parameters = parenthesizedLambdaSyntax.ParameterList.Parameters;
                if (parameters.Count > ordinalInLambda)
                {
                    var parameter = parameters[ordinalInLambda];
                    if (parameter.Type != null)
                    {
                        explicitLambdaParameterType = _context.SemanticModel.GetTypeInfo(parameter.Type, _cancellationToken).Type;
                        return explicitLambdaParameterType != null;
                    }
                }
            }

            // Non-parenthesized lambdas cannot explicitly specify the type of the single parameter
            explicitLambdaParameterType = null;
            return false;
        }

        private ImmutableArray<ISymbol> GetSymbolsForCurrentContext()
        {
            if (_context.IsGlobalStatementContext)
            {
                // Script and interactive
                return GetSymbolsForGlobalStatementContext();
            }
            else if (_context.IsAnyExpressionContext ||
                     _context.IsStatementContext ||
                     _context.SyntaxTree.IsDefiniteCastTypeContext(_context.Position, _context.LeftToken))
            {
                // GitHub #717: With automatic brace completion active, typing '(i' produces "(i)", which gets parsed as
                // as cast. The user might be trying to type a parenthesized expression, so even though a cast
                // is a type-only context, we'll show all symbols anyway.
                return GetSymbolsForExpressionOrStatementContext();
            }
            else if (_context.IsTypeContext || _context.IsNamespaceContext)
            {
                return GetSymbolsForTypeOrNamespaceContext();
            }
            else if (_context.IsLabelContext)
            {
                return GetSymbolsForLabelContext();
            }
            else if (_context.IsTypeArgumentOfConstraintContext)
            {
                return GetSymbolsForTypeArgumentOfConstraintClause();
            }
            else if (_context.IsDestructorTypeContext)
            {
                var symbol = _context.SemanticModel.GetDeclaredSymbol(_context.ContainingTypeOrEnumDeclaration!, _cancellationToken);
                return symbol == null ? ImmutableArray<ISymbol>.Empty : ImmutableArray.Create<ISymbol>(symbol);
            }
            else if (_context.IsNamespaceDeclarationNameContext)
            {
                return GetSymbolsForNamespaceDeclarationNameContext<NamespaceDeclarationSyntax>();
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        private ImmutableArray<ISymbol> GetSymbolsOffOfContainer()
        {
            // Ensure that we have the correct token in A.B| case
            var node = _context.TargetToken.GetRequiredParent();
            return node switch
            {
                MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } memberAccess
                    => GetSymbolsOffOfExpression(memberAccess.Expression),
                MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.PointerMemberAccessExpression } memberAccess
                    => GetSymbolsOffOfDereferencedExpression(memberAccess.Expression),

                // This code should be executing only if the cursor is between two dots in a dotdot token.
                RangeExpressionSyntax rangeExpression => GetSymbolsOffOfExpression(rangeExpression.LeftOperand),
                QualifiedNameSyntax qualifiedName => GetSymbolsOffOfName(qualifiedName.Left),
                AliasQualifiedNameSyntax aliasName => GetSymbolsOffOffAlias(aliasName.Alias),
                MemberBindingExpressionSyntax _ => GetSymbolsOffOfConditionalReceiver(node.GetParentConditionalAccessExpression()!.Expression),
                _ => ImmutableArray<ISymbol>.Empty,
            };
        }

        private ImmutableArray<ISymbol> GetSymbolsForGlobalStatementContext()
        {
            var syntaxTree = _context.SyntaxTree;
            var position = _context.Position;
            var token = _context.LeftToken;

            // The following code is a hack to get around a binding problem when asking binding
            // questions immediately after a using directive. This is special-cased in the binder
            // factory to ensure that using directives are not within scope inside other using
            // directives. That generally works fine for .cs, but it's a problem for interactive
            // code in this case:
            //
            // using System;
            // |

            if (token.Kind() == SyntaxKind.SemicolonToken &&
                token.Parent.IsKind(SyntaxKind.UsingDirective) &&
                position >= token.Span.End)
            {
                var compUnit = (CompilationUnitSyntax)syntaxTree.GetRoot(_cancellationToken);
                if (compUnit.Usings.Count > 0 && compUnit.Usings.Last().GetLastToken() == token)
                {
                    token = token.GetNextToken(includeZeroWidth: true);
                }
            }

            var symbols = _context.SemanticModel.LookupSymbols(token.SpanStart);

            return symbols;
        }

        private ImmutableArray<ISymbol> GetSymbolsForTypeArgumentOfConstraintClause()
        {
            var enclosingSymbol = _context.LeftToken.GetRequiredParent()
                .AncestorsAndSelf()
                .Select(n => _context.SemanticModel.GetDeclaredSymbol(n, _cancellationToken))
                .WhereNotNull()
                .FirstOrDefault();

            var symbols = enclosingSymbol != null
                ? enclosingSymbol.GetTypeArguments()
                : ImmutableArray<ITypeSymbol>.Empty;

            return ImmutableArray<ISymbol>.CastUp(symbols);
        }

        private ImmutableArray<ISymbol> GetSymbolsOffOffAlias(IdentifierNameSyntax alias)
        {
            var aliasSymbol = _context.SemanticModel.GetAliasInfo(alias, _cancellationToken);
            if (aliasSymbol == null)
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            return _context.SemanticModel.LookupNamespacesAndTypes(
                alias.SpanStart,
                aliasSymbol.Target);
        }

        private ImmutableArray<ISymbol> GetSymbolsForLabelContext()
        {
            var allLabels = _context.SemanticModel.LookupLabels(_context.LeftToken.SpanStart);

            // Exclude labels (other than 'default') that come from case switch statements

            return allLabels
                .WhereAsArray(label => label.DeclaringSyntaxReferences.First().GetSyntax(_cancellationToken)
                    .IsKind(SyntaxKind.LabeledStatement, SyntaxKind.DefaultSwitchLabel));
        }

        private ImmutableArray<ISymbol> GetSymbolsForTypeOrNamespaceContext()
        {
            var symbols = _context.SemanticModel.LookupNamespacesAndTypes(_context.LeftToken.SpanStart);

            if (_context.TargetToken.IsUsingKeywordInUsingDirective())
            {
                return symbols.WhereAsArray(s => s.IsNamespace());
            }

            if (_context.TargetToken.IsStaticKeywordInUsingDirective())
            {
                return symbols.WhereAsArray(s => !s.IsDelegateType() && !s.IsInterfaceType());
            }

            return symbols;
        }

        private ImmutableArray<ISymbol> GetSymbolsForExpressionOrStatementContext()
        {
            // Check if we're in an interesting situation like this:
            //
            //     i          // <-- here
            //     I = 0;

            // The problem is that "i I = 0" causes a local to be in scope called "I".  So, later when
            // we look up symbols, it masks any other 'I's in scope (i.e. if there's a field with that 
            // name).  If this is the case, we do not want to filter out inaccessible locals.
            var filterOutOfScopeLocals = _filterOutOfScopeLocals;
            if (filterOutOfScopeLocals)
                filterOutOfScopeLocals = !_context.LeftToken.GetRequiredParent().IsFoundUnder<LocalDeclarationStatementSyntax>(d => d.Declaration.Type);

            var symbols = !_context.IsNameOfContext && _context.LeftToken.GetRequiredParent().IsInStaticContext()
                ? _context.SemanticModel.LookupStaticMembers(_context.LeftToken.SpanStart)
                : _context.SemanticModel.LookupSymbols(_context.LeftToken.SpanStart);

            // Filter out any extension methods that might be imported by a using static directive.
            // But include extension methods declared in the context's type or it's parents
            var contextOuterTypes = _context.GetOuterTypes(_cancellationToken);
            var contextEnclosingNamedType = _context.SemanticModel.GetEnclosingNamedType(_context.Position, _cancellationToken);

            symbols = symbols.WhereAsArray(symbol =>
                !symbol.IsExtensionMethod() ||
                Equals(contextEnclosingNamedType, symbol.ContainingType) ||
                contextOuterTypes.Any(outerType => outerType.Equals(symbol.ContainingType)));

            // The symbols may include local variables that are declared later in the method and
            // should not be included in the completion list, so remove those. Filter them away,
            // unless we're in the debugger, where we show all locals in scope.
            if (filterOutOfScopeLocals)
            {
                symbols = symbols.WhereAsArray(symbol => !symbol.IsInaccessibleLocal(_context.Position));
            }

            return symbols;
        }

        private ImmutableArray<ISymbol> GetSymbolsOffOfName(NameSyntax name)
        {
            // Using an is pattern on an enum is a qualified name, but normal symbol processing works fine
            if (_context.IsEnumTypeMemberAccessContext)
            {
                return GetSymbolsOffOfExpression(name);
            }

            // Check if we're in an interesting situation like this:
            //
            //     int i = 5;
            //     i.          // <-- here
            //     List<string> ml = new List<string>();
            //
            // The problem is that "i.List<string>" gets parsed as a type.  In this case we need 
            // to try binding again as if "i" is an expression and not a type.  In order to do 
            // that, we need to speculate as to what 'i' meant if it wasn't part of a local 
            // declaration's type.
            //
            // Another interesting case is something like:
            //
            //      stringList.
            //      await Test2();
            //
            // Here "stringList.await" is thought of as the return type of a local function.

            if (name.IsFoundUnder<LocalFunctionStatementSyntax>(d => d.ReturnType) ||
                name.IsFoundUnder<LocalDeclarationStatementSyntax>(d => d.Declaration.Type) ||
                name.IsFoundUnder<FieldDeclarationSyntax>(d => d.Declaration.Type))
            {
                var speculativeBinding = _context.SemanticModel.GetSpeculativeSymbolInfo(
                    name.SpanStart, name, SpeculativeBindingOption.BindAsExpression);

                var container = _context.SemanticModel.GetSpeculativeTypeInfo(
                    name.SpanStart, name, SpeculativeBindingOption.BindAsExpression).Type;

                var speculativeResult = GetSymbolsOffOfBoundExpression(name, name, speculativeBinding, container);

                return speculativeResult;
            }

            // We're in a name-only context, since if we were an expression we'd be a
            // MemberAccessExpressionSyntax. Thus, let's do other namespaces and types.
            var nameBinding = _context.SemanticModel.GetSymbolInfo(name, _cancellationToken);

            if (nameBinding.Symbol is INamespaceOrTypeSymbol symbol)
            {
                if (_context.IsNameOfContext)
                {
                    return _context.SemanticModel.LookupSymbols(position: name.SpanStart, container: symbol);
                }

                var symbols = _context.SemanticModel.LookupNamespacesAndTypes(
                    position: name.SpanStart,
                    container: symbol);

                if (_context.IsNamespaceDeclarationNameContext)
                {
                    var declarationSyntax = name.GetAncestorOrThis<NamespaceDeclarationSyntax>();
                    return symbols.WhereAsArray(s => IsNonIntersectingNamespace(s, declarationSyntax));
                }

                // Filter the types when in a using directive, but not an alias.
                // 
                // Cases:
                //    using | -- Show namespaces
                //    using A.| -- Show namespaces
                //    using static | -- Show namespace and types
                //    using A = B.| -- Show namespace and types
                var usingDirective = name.GetAncestorOrThis<UsingDirectiveSyntax>();
                if (usingDirective != null && usingDirective.Alias == null)
                {
                    return usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                        ? symbols.WhereAsArray(s => !s.IsDelegateType() && !s.IsInterfaceType())
                        : symbols.WhereAsArray(s => s.IsNamespace());
                }

                return symbols;
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        private ImmutableArray<ISymbol> GetSymbolsOffOfExpression(ExpressionSyntax? originalExpression)
        {
            if (originalExpression == null)
                return ImmutableArray<ISymbol>.Empty;

            // In case of 'await x$$', we want to move to 'x' to get it's members.
            // To run GetSymbolInfo, we also need to get rid of parenthesis.
            var expression = originalExpression is AwaitExpressionSyntax awaitExpression
                ? awaitExpression.Expression.WalkDownParentheses()
                : originalExpression.WalkDownParentheses();

            var leftHandBinding = _context.SemanticModel.GetSymbolInfo(expression, _cancellationToken);
            var container = _context.SemanticModel.GetTypeInfo(expression, _cancellationToken).Type;

            var normalSymbols = GetSymbolsOffOfBoundExpression(originalExpression, expression, leftHandBinding, container);

            // Check for the Color Color case.
            if (originalExpression.CanAccessInstanceAndStaticMembersOffOf(_context.SemanticModel, _cancellationToken))
            {
                var speculativeSymbolInfo = _context.SemanticModel.GetSpeculativeSymbolInfo(expression.SpanStart, expression, SpeculativeBindingOption.BindAsTypeOrNamespace);

                var typeMembers = GetSymbolsOffOfBoundExpression(originalExpression, expression, speculativeSymbolInfo, container);

                normalSymbols = normalSymbols.Concat(typeMembers);
            }

            return normalSymbols;
        }

        private ImmutableArray<ISymbol> GetSymbolsOffOfDereferencedExpression(ExpressionSyntax originalExpression)
        {
            var expression = originalExpression.WalkDownParentheses();
            var leftHandBinding = _context.SemanticModel.GetSymbolInfo(expression, _cancellationToken);

            var container = _context.SemanticModel.GetTypeInfo(expression, _cancellationToken).Type;
            if (container is IPointerTypeSymbol pointerType)
            {
                container = pointerType.PointedAtType;
            }

            return GetSymbolsOffOfBoundExpression(originalExpression, expression, leftHandBinding, container);
        }

        private ImmutableArray<ISymbol> GetSymbolsOffOfConditionalReceiver(ExpressionSyntax originalExpression)
        {
            // Given ((T?)t)?.|, the '.' will behave as if the expression was actually ((T)t).|. More plainly,
            // a member access off of a conditional receiver of nullable type binds to the unwrapped nullable
            // type. This is not exposed via the binding information for the LHS, so repeat this work here.

            var expression = originalExpression.WalkDownParentheses();
            var leftHandBinding = _context.SemanticModel.GetSymbolInfo(expression, _cancellationToken);
            var container = _context.SemanticModel.GetTypeInfo(expression, _cancellationToken).Type.RemoveNullableIfPresent();

            // If the thing on the left is a type, namespace, or alias, we shouldn't show anything in
            // IntelliSense.
            if (leftHandBinding.GetBestOrAllSymbols().FirstOrDefault().MatchesKind(SymbolKind.NamedType, SymbolKind.Namespace, SymbolKind.Alias))
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            return GetSymbolsOffOfBoundExpression(originalExpression, expression, leftHandBinding, container);
        }

        private ImmutableArray<ISymbol> GetSymbolsOffOfBoundExpression(
            ExpressionSyntax originalExpression,
            ExpressionSyntax expression,
            SymbolInfo leftHandBinding,
            ITypeSymbol? containerType)
        {
            var excludeInstance = false;
            var excludeStatic = true;

            ISymbol? containerSymbol = containerType;

            var symbol = leftHandBinding.GetAnySymbol();
            if (symbol != null)
            {
                // If the thing on the left is a lambda expression, we shouldn't show anything.
                if (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction })
                    return ImmutableArray<ISymbol>.Empty;

                var originalExpressionKind = originalExpression.Kind();

                // If the thing on the left is a type, namespace or alias and the original
                // expression was parenthesized, we shouldn't show anything in IntelliSense.
                if (originalExpressionKind is SyntaxKind.ParenthesizedExpression &&
                    symbol.Kind is SymbolKind.NamedType or SymbolKind.Namespace or SymbolKind.Alias)
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                // If the thing on the left is a method name identifier, we shouldn't show anything.
                if (symbol.Kind is SymbolKind.Method &&
                    originalExpressionKind is SyntaxKind.IdentifierName or SyntaxKind.GenericName)
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                // If the thing on the left is an event that can't be used as a field, we shouldn't show anything
                if (symbol is IEventSymbol ev &&
                    !_context.SemanticModel.IsEventUsableAsField(originalExpression.SpanStart, ev))
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                if (symbol is IAliasSymbol alias)
                    symbol = alias.Target;

                if (symbol.Kind is SymbolKind.NamedType or SymbolKind.Namespace)
                {
                    // For named typed and namespaces, we flip things around.  We only want statics and not instance members.
                    excludeInstance = true;
                    excludeStatic = false;
                    containerSymbol = (INamespaceOrTypeSymbol)symbol;
                }

                // Special case parameters. If we have a normal (non this/base) parameter, then that's what we want to
                // lookup symbols off of as we have a lot of special logic for determining member symbols of lambda
                // parameters.
                //
                // If it is a this/base parameter and we're in a static context, we shouldn't show anything
                if (symbol is IParameterSymbol parameter)
                {
                    if (parameter.IsThis && expression.IsInStaticContext())
                        return ImmutableArray<ISymbol>.Empty;

                    containerSymbol = symbol;
                }
            }
            else if (containerType != null)
            {
                // Otherwise, if it wasn't a symbol on the left, but it was something that had a type,
                // then include instance members for it.
                excludeStatic = true;
            }

            if (containerSymbol == null)
                return ImmutableArray<ISymbol>.Empty;

            Debug.Assert(!excludeInstance || !excludeStatic);

            // nameof(X.|
            // Show static and instance members.
            if (_context.IsNameOfContext)
            {
                excludeInstance = false;
                excludeStatic = false;
            }

            var useBaseReferenceAccessibility = symbol is IParameterSymbol { IsThis: true } p && !p.Type.Equals(containerType);
            var symbols = GetMemberSymbols(containerSymbol, position: originalExpression.SpanStart, excludeInstance, useBaseReferenceAccessibility);

            // If we're showing instance members, don't include nested types
            return excludeStatic
                ? symbols.WhereAsArray(s => !(s.IsStatic || s is ITypeSymbol))
                : symbols;
        }
    }
}
