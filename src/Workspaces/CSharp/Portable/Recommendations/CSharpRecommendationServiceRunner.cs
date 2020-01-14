// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
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
                return ImmutableArray.Create<ISymbol>(
                    _context.SemanticModel.GetDeclaredSymbol(_context.ContainingTypeOrEnumDeclaration, _cancellationToken));
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
            var node = _context.TargetToken.Parent;

            if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                return GetSymbolsOffOfExpression(((MemberAccessExpressionSyntax)node).Expression);
            }
            else if (node.Kind() == SyntaxKind.RangeExpression)
            {
                // This code should be executing only if the cursor is between two dots in a dotdot token.
                return GetSymbolsOffOfExpression(((RangeExpressionSyntax)node).LeftOperand);
            }
            else if (node.Kind() == SyntaxKind.PointerMemberAccessExpression)
            {
                return GetSymbolsOffOfDereferencedExpression(((MemberAccessExpressionSyntax)node).Expression);
            }
            else if (node.Kind() == SyntaxKind.QualifiedName)
            {
                return GetSymbolsOffOfName(((QualifiedNameSyntax)node).Left);
            }
            else if (node.Kind() == SyntaxKind.AliasQualifiedName)
            {
                return GetSymbolsOffOffAlias(((AliasQualifiedNameSyntax)node).Alias);
            }
            else if (node.Kind() == SyntaxKind.MemberBindingExpression)
            {
                var parentConditionalAccess = node.GetParentConditionalAccessExpression();
                return GetSymbolsOffOfConditionalReceiver(parentConditionalAccess.Expression);
            }
            else
            {
                return ImmutableArray<ISymbol>.Empty;
            }
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
            var enclosingSymbol = _context.LeftToken.Parent
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
            {
                if (_context.LeftToken.Parent.IsFoundUnder<LocalDeclarationStatementSyntax>(d => d.Declaration.Type))
                {
                    filterOutOfScopeLocals = false;
                }
            }

            var symbols = !_context.IsNameOfContext && _context.LeftToken.Parent.IsInStaticContext()
                ? _context.SemanticModel.LookupStaticMembers(_context.LeftToken.SpanStart)
                : _context.SemanticModel.LookupSymbols(_context.LeftToken.SpanStart);

            // Filter out any extension methods that might be imported by a using static directive.
            // But include extension methods declared in the context's type or it's parents
            var contextEnclosingNamedType = _context.SemanticModel.GetEnclosingNamedType(_context.Position, _cancellationToken);
            var contextOuterTypes = _context.GetOuterTypes(_cancellationToken);
            symbols = symbols.WhereAsArray(symbol =>
                !symbol.IsExtensionMethod() ||
                contextEnclosingNamedType.Equals(symbol.ContainingType) ||
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

        private ImmutableArray<ISymbol> GetSymbolsOffOfExpression(ExpressionSyntax originalExpression)
        {
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
            INamespaceOrTypeSymbol container)
        {
            var useBaseReferenceAccessibility = false;
            var excludeInstance = false;
            var excludeStatic = false;
            var symbol = leftHandBinding.GetAnySymbol();
            ImmutableArray<ISymbol> symbols = default;

            if (symbol != null)
            {
                // If the thing on the left is a type, namespace or alias and the original
                // expression was parenthesized, we shouldn't show anything in IntelliSense.
                if (originalExpression.IsKind(SyntaxKind.ParenthesizedExpression) &&
                    symbol.MatchesKind(SymbolKind.NamedType,
                                       SymbolKind.Namespace,
                                       SymbolKind.Alias))
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                // If the thing on the left is a lambda expression, we shouldn't show anything.
                if (symbol.Kind == SymbolKind.Method &&
                    ((IMethodSymbol)symbol).MethodKind == MethodKind.AnonymousFunction)
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                // If the thing on the left is a method name identifier, we shouldn't show anything.
                var originalExpressionKind = originalExpression.Kind();
                if (symbol.Kind == SymbolKind.Method &&
                    (originalExpressionKind == SyntaxKind.IdentifierName || originalExpressionKind == SyntaxKind.GenericName))
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                // If the thing on the left is an event that can't be used as a field, we shouldn't show anything
                if (symbol.Kind == SymbolKind.Event &&
                    !_context.SemanticModel.IsEventUsableAsField(originalExpression.SpanStart, (IEventSymbol)symbol))
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                // If the thing on the left is a this parameter (e.g. this or base) and we're in a static context,
                // we shouldn't show anything
                if (symbol.IsThisParameter() &&
                    expression.IsInStaticContext())
                {
                    return ImmutableArray<ISymbol>.Empty;
                }

                // What is the thing on the left?
                switch (symbol.Kind)
                {
                    case SymbolKind.NamedType:
                    case SymbolKind.Namespace:
                        excludeInstance = true;
                        container = (INamespaceOrTypeSymbol)symbol;
                        break;

                    case SymbolKind.Alias:
                        excludeInstance = true;
                        container = ((IAliasSymbol)symbol).Target;
                        break;

                    case SymbolKind.Parameter:
                        var parameter = (IParameterSymbol)symbol;

                        excludeStatic = true;

                        symbols = GetSymbols(parameter, originalExpression.SpanStart);

                        // case:
                        //    base.|
                        if (parameter.IsThis && !object.Equals(parameter.Type, container))
                        {
                            useBaseReferenceAccessibility = true;
                        }

                        break;

                    default:
                        excludeStatic = true;
                        break;
                }
            }
            else if (container != null)
            {
                excludeStatic = true;
            }
            else
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            Debug.Assert(!excludeInstance || !excludeStatic);
            Debug.Assert(!excludeInstance || !useBaseReferenceAccessibility);

            // nameof(X.|
            // Show static and instance members.
            if (_context.IsNameOfContext)
            {
                excludeInstance = false;
                excludeStatic = false;
            }

            if (symbols.IsDefault)
            {
                symbols = GetSymbols(
                    container,
                    position: originalExpression.SpanStart,
                    excludeInstance: excludeInstance,
                    useBaseReferenceAccessibility: useBaseReferenceAccessibility);
            }

            // If we're showing instance members, don't include nested types
            return excludeStatic
                ? symbols.WhereAsArray(s => !s.IsStatic && !(s is ITypeSymbol))
                : symbols;
        }
    }
}
