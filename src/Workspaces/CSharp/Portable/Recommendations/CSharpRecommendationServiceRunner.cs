// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations;

internal partial class CSharpRecommendationService
{
    private sealed partial class CSharpRecommendationServiceRunner : AbstractRecommendationServiceRunner
    {
        public CSharpRecommendationServiceRunner(
            CSharpSyntaxContext context, bool filterOutOfScopeLocals, CancellationToken cancellationToken)
            : base(context, filterOutOfScopeLocals, cancellationToken)
        {
        }

        protected override int GetLambdaParameterCount(AnonymousFunctionExpressionSyntax lambdaSyntax)
            => lambdaSyntax switch
            {
                AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.ParameterList?.Parameters.Count ?? -1,
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters.Count,
                SimpleLambdaExpressionSyntax => 1,
                _ => throw ExceptionUtilities.UnexpectedValue(lambdaSyntax.Kind()),
            };

        public override RecommendedSymbols GetRecommendedSymbols()
        {
            if (_context.IsInNonUserCode ||
                _context.IsPreProcessorDirectiveContext)
            {
                return default;
            }

            if (!_context.IsRightOfNameSeparator)
                return new RecommendedSymbols(GetSymbolsForCurrentContext());

            return GetSymbolsOffOfContainer();
        }

        public override bool TryGetExplicitTypeOfLambdaParameter(SyntaxNode lambdaSyntax, int ordinalInLambda, [NotNullWhen(true)] out ITypeSymbol? explicitLambdaParameterType)
        {
            if (lambdaSyntax is ParenthesizedLambdaExpressionSyntax parenthesizedLambdaSyntax)
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
                // Script, interactive, or top-level statement
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
                return GetSymbolsForNamespaceDeclarationNameContext<BaseNamespaceDeclarationSyntax>();
            }
            else if (_context.IsEnumBaseListContext)
            {
                return GetSymbolsForEnumBaseList(container: null);
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        private RecommendedSymbols GetSymbolsOffOfContainer()
        {
            // Ensure that we have the correct token in A.B| case
            var node = _context.TargetToken.GetRequiredParent();

            if (node.GetAncestor<BaseListSyntax>()?.Parent is EnumDeclarationSyntax)
            {
                // We are in enum's base list. Valid nodes here are:
                // 1) QualifiedNameSyntax, e.g. `enum E : System.$$`
                // 2) AliasQualifiedNameSyntax, e.g. `enum E : global::$$`
                // If there is anything else then this is not valid syntax, so just return empty recommendations
                if (node is not (QualifiedNameSyntax or AliasQualifiedNameSyntax))
                {
                    return default;
                }
            }

            return node switch
            {
                MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression) memberAccess
                    => GetSymbolsOffOfExpression(memberAccess.Expression),
                MemberAccessExpressionSyntax(SyntaxKind.PointerMemberAccessExpression) memberAccess
                    => GetSymbolsOffOfDereferencedExpression(memberAccess.Expression),

                // This code should be executing only if the cursor is between two dots in a `..` token.
                RangeExpressionSyntax rangeExpression => GetSymbolsOffOfRangeExpression(rangeExpression),
                QualifiedNameSyntax qualifiedName => GetSymbolsOffOfName(qualifiedName.Left),
                AliasQualifiedNameSyntax aliasName => GetSymbolsOffOffAlias(aliasName.Alias),
                MemberBindingExpressionSyntax => GetSymbolsOffOfConditionalReceiver(node.GetParentConditionalAccessExpression()!.Expression),
                _ => default,
            };
        }

        private RecommendedSymbols GetSymbolsOffOfRangeExpression(RangeExpressionSyntax rangeExpression)
        {
            // This commonly occurs when someone has existing dots and types another dot to bring up completion. For example:
            //
            //      collection$$.Any()
            //
            // producing
            //
            //      collection..Any();
            //
            // We can get good completion by just getting symbols off of 'collection' there, but with a small catch.
            // Specifically, we only want to allow this if the precedence would allow for a member-access-expression
            // here.  This is because the range-expression is much lower precedence so it allows for all sorts of
            // expressions on the LHS that would not parse into member access expression.
            //
            // Note: This can get complex because of cases like   `(int)o..Whatever();`
            //
            // Here, we want completion off of `o`, despite the LHS being the entire `(int)o` expr.  So we attempt to
            // walk down the RHS of the expression before the .., looking to get the final term that the `.` should
            // actually bind to.

            var currentExpression = rangeExpression.LeftOperand;
            if (currentExpression is not null)
            {
                while (currentExpression.ChildNodesAndTokens().Last().AsNode() is ExpressionSyntax child &&
                       child.GetOperatorPrecedence() < OperatorPrecedence.Primary)
                {
                    currentExpression = child;
                }

                var precedence = currentExpression.GetOperatorPrecedence();
                if (precedence != OperatorPrecedence.None && precedence < OperatorPrecedence.Primary)
                    return default;
            }

            return GetSymbolsOffOfExpression(currentExpression);
        }

        private ImmutableArray<ISymbol> GetSymbolsForGlobalStatementContext()
        {
            var token = _context.TargetToken;

            // The following code is a hack to get around a binding problem when asking binding
            // questions immediately after a using directive. This is special-cased in the binder
            // factory to ensure that using directives are not within scope inside other using
            // directives. That generally works fine for .cs, but it's a problem for interactive
            // code in this case:
            //
            // using System;
            // |

            if (_context.IsRightAfterUsingOrImportDirective)
                token = token.GetNextToken(includeZeroWidth: true);

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

        private RecommendedSymbols GetSymbolsOffOffAlias(IdentifierNameSyntax alias)
        {
            var aliasSymbol = _context.SemanticModel.GetAliasInfo(alias, _cancellationToken);
            if (aliasSymbol == null)
                return default;

            // If we are in case like `enum E : global::$$` we need to show only `System` namespace
            if (alias.GetAncestor<BaseListSyntax>()?.Parent is EnumDeclarationSyntax)
                return new(GetSymbolsForEnumBaseList(aliasSymbol.Target));

            return new RecommendedSymbols(_context.SemanticModel.LookupNamespacesAndTypes(
                alias.SpanStart,
                aliasSymbol.Target));
        }

        private ImmutableArray<ISymbol> GetSymbolsForLabelContext()
        {
            var allLabels = _context.SemanticModel.LookupLabels(_context.LeftToken.SpanStart);

            // Exclude labels (other than 'default') that come from case switch statements

            return allLabels
                .WhereAsArray(label => label.DeclaringSyntaxReferences.First().GetSyntax(_cancellationToken)
                    .Kind() is SyntaxKind.LabeledStatement or SyntaxKind.DefaultSwitchLabel);
        }

        private ImmutableArray<ISymbol> GetSymbolsForTypeOrNamespaceContext()
        {
            var symbols = _context.SemanticModel.LookupNamespacesAndTypes(_context.LeftToken.SpanStart);

            if (_context.TargetToken.IsUsingKeywordInUsingDirective())
            {
                return symbols.WhereAsArray(s => s.IsNamespace());
            }

            if (_context.TargetToken.IsStaticKeywordContextInUsingDirective())
            {
                return symbols.WhereAsArray(s => !s.IsDelegateType());
            }

            return symbols;
        }

        private ImmutableArray<ISymbol> GetSymbolsForExpressionOrStatementContext()
        {
            // Check if we're in an interesting situation like this:
            //
            //     i          // <-- here
            //     I = 0;
            //
            // The problem is that "i I = 0" causes a local to be in scope called "I".  So, later when
            // we look up symbols, it masks any other 'I's in scope (i.e. if there's a field with that 
            // name).  If this is the case, we do not want to filter out inaccessible locals.
            //
            // Similar issue for out-vars.  Like:
            //
            //              if (TryParse("", out    // <-- here
            //              X x = null;
            var filterOutOfScopeLocals = _filterOutOfScopeLocals;
            if (filterOutOfScopeLocals)
            {
                var contextNode = _context.LeftToken.GetRequiredParent();
                filterOutOfScopeLocals =
                    !contextNode.IsFoundUnder<LocalDeclarationStatementSyntax>(d => d.Declaration.Type) &&
                    !contextNode.IsFoundUnder<DeclarationExpressionSyntax>(d => d.Type);
            }

            var symbols = !_context.IsNameOfContext && _context.LeftToken.GetRequiredParent().IsInStaticContext()
                ? _context.SemanticModel.LookupStaticMembers(_context.LeftToken.SpanStart)
                : _context.SemanticModel.LookupSymbols(_context.LeftToken.SpanStart);

            // filter our top level locals if we're inside a type declaration.
            if (_context.ContainingTypeDeclaration != null)
                symbols = symbols.WhereAsArray(s => s.ContainingSymbol.Name != WellKnownMemberNames.TopLevelStatementsEntryPointMethodName);

            // Filter out any extension methods that might be imported by a using static directive.
            // But include extension methods declared in the context's type or it's parents
            var contextOuterTypes = ComputeOuterTypes(_context, _cancellationToken);
            var contextEnclosingNamedType = _context.SemanticModel.GetEnclosingNamedType(_context.Position, _cancellationToken);

            symbols = symbols.WhereAsArray(symbol =>
                !symbol.IsExtensionMethod() ||
                Equals(contextEnclosingNamedType, symbol.ContainingType) ||
                contextOuterTypes.Any(outerType => outerType.Equals(symbol.ContainingType)));

            // The symbols may include local variables that are declared later in the method and
            // should not be included in the completion list, so remove those. Filter them away,
            // unless we're in the debugger, where we show all locals in scope.
            if (filterOutOfScopeLocals)
                symbols = symbols.WhereAsArray(symbol => !symbol.IsInaccessibleLocal(_context.Position));

            return symbols;
        }

        private RecommendedSymbols GetSymbolsOffOfName(NameSyntax name)
        {
            // Using an is pattern on an enum is a qualified name, but normal symbol processing works fine
            if (_context.IsEnumTypeMemberAccessContext)
                return GetSymbolsOffOfExpression(name);

            if (name.ShouldNameExpressionBeTreatedAsExpressionInsteadOfType(_context.SemanticModel, out var nameBinding, out var container))
                return GetSymbolsOffOfBoundExpression(name, name, nameBinding, container, unwrapNullable: false, isForDereference: false);

            // We're in a name-only context, since if we were an expression we'd be a
            // MemberAccessExpressionSyntax. Thus, let's do other namespaces and types.
            nameBinding = _context.SemanticModel.GetSymbolInfo(name, _cancellationToken);
            if (nameBinding.Symbol is not INamespaceOrTypeSymbol symbol)
                return default;

            if (_context.IsNameOfContext)
                return new RecommendedSymbols(_context.SemanticModel.LookupSymbols(position: name.SpanStart, container: symbol));

            if (name.GetAncestor<BaseListSyntax>()?.Parent is EnumDeclarationSyntax)
                return new(GetSymbolsForEnumBaseList(symbol));

            var symbols = _context.SemanticModel.LookupNamespacesAndTypes(
                position: name.SpanStart,
                container: symbol);

            if (_context.IsNamespaceDeclarationNameContext)
            {
                var declarationSyntax = name.GetAncestorOrThis<BaseNamespaceDeclarationSyntax>();
                return new RecommendedSymbols(symbols.WhereAsArray(s => IsNonIntersectingNamespace(s, declarationSyntax)));
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
                return new RecommendedSymbols(usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                    ? symbols.WhereAsArray(s => !s.IsDelegateType())
                    : symbols.WhereAsArray(s => s.IsNamespace()));
            }

            return new RecommendedSymbols(symbols);
        }

        private RecommendedSymbols GetSymbolsOffOfExpression(ExpressionSyntax? originalExpression)
        {
            if (originalExpression == null)
                return default;

            // In case of 'await x$$', we want to move to 'x' to get it's members.
            // To run GetSymbolInfo, we also need to get rid of parenthesis.
            var expression = originalExpression is AwaitExpressionSyntax awaitExpression
                ? awaitExpression.Expression.WalkDownParentheses()
                : originalExpression.WalkDownParentheses();

            var leftHandBinding = _context.SemanticModel.GetSymbolInfo(expression, _cancellationToken);
            var container = _context.SemanticModel.GetTypeInfo(expression, _cancellationToken).Type;

            var result = GetSymbolsOffOfBoundExpression(originalExpression, expression, leftHandBinding, container, unwrapNullable: false, isForDereference: false);

            // Check for the Color Color case.
            if (originalExpression.CanAccessInstanceAndStaticMembersOffOf(_context.SemanticModel, _cancellationToken))
            {
                var speculativeSymbolInfo = _context.SemanticModel.GetSpeculativeSymbolInfo(expression.SpanStart, expression, SpeculativeBindingOption.BindAsTypeOrNamespace);

                var typeMembers = GetSymbolsOffOfBoundExpression(originalExpression, expression, speculativeSymbolInfo, container, unwrapNullable: false, isForDereference: false);

                result = new RecommendedSymbols(
                    result.NamedSymbols.Concat(typeMembers.NamedSymbols),
                    result.UnnamedSymbols);
            }

            return result;
        }

        private RecommendedSymbols GetSymbolsOffOfDereferencedExpression(ExpressionSyntax originalExpression)
        {
            var expression = originalExpression.WalkDownParentheses();
            var leftHandBinding = _context.SemanticModel.GetSymbolInfo(expression, _cancellationToken);
            var container = _context.SemanticModel.GetTypeInfo(expression, _cancellationToken).Type;

            return GetSymbolsOffOfBoundExpression(originalExpression, expression, leftHandBinding, container, unwrapNullable: false, isForDereference: true);
        }

        private RecommendedSymbols GetSymbolsOffOfConditionalReceiver(ExpressionSyntax originalExpression)
        {
            // Given ((T?)t)?.|, the '.' will behave as if the expression was actually ((T)t).|. More plainly,
            // a member access off of a conditional receiver of nullable type binds to the unwrapped nullable
            // type. This is not exposed via the binding information for the LHS, so repeat this work here.

            var expression = originalExpression.WalkDownParentheses();
            var leftHandBinding = _context.SemanticModel.GetSymbolInfo(expression, _cancellationToken);
            var container = _context.SemanticModel.GetTypeInfo(expression, _cancellationToken).Type;

            // If the thing on the left is a type, namespace, or alias, we shouldn't show anything in
            // IntelliSense.
            if (leftHandBinding.GetBestOrAllSymbols().FirstOrDefault().MatchesKind(SymbolKind.NamedType, SymbolKind.Namespace, SymbolKind.Alias))
                return default;

            return GetSymbolsOffOfBoundExpression(originalExpression, expression, leftHandBinding, container, unwrapNullable: true, isForDereference: false);
        }

        private RecommendedSymbols GetSymbolsOffOfBoundExpression(
            ExpressionSyntax originalExpression,
            ExpressionSyntax expression,
            SymbolInfo leftHandBinding,
            ITypeSymbol? containerType,
            bool unwrapNullable,
            bool isForDereference)
        {
            var excludeInstance = false;
            var excludeStatic = true;
            var excludeBaseMethodsForRefStructs = true;

            ISymbol? containerSymbol = containerType;

            var symbol = leftHandBinding.GetAnySymbol();
            if (symbol != null)
            {
                // If the thing on the left is a lambda expression, we shouldn't show anything.
                if (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction })
                    return default;

                var originalExpressionKind = originalExpression.Kind();

                // If the thing on the left is a type, namespace or alias and the original
                // expression was parenthesized, we shouldn't show anything in IntelliSense.
                if (originalExpressionKind is SyntaxKind.ParenthesizedExpression &&
                    symbol.Kind is SymbolKind.NamedType or SymbolKind.Namespace or SymbolKind.Alias)
                {
                    return default;
                }

                // If the thing on the left is a method name identifier, we shouldn't show anything.
                if (symbol.Kind is SymbolKind.Method &&
                    originalExpressionKind is SyntaxKind.IdentifierName or SyntaxKind.GenericName)
                {
                    return default;
                }

                // If the thing on the left is an event that can't be used as a field, we shouldn't show anything
                if (symbol is IEventSymbol ev &&
                    !_context.SemanticModel.IsEventUsableAsField(originalExpression.SpanStart, ev))
                {
                    return default;
                }

                if (symbol is IAliasSymbol alias)
                    symbol = alias.Target;

                if (symbol.Kind is SymbolKind.NamedType or SymbolKind.Namespace or SymbolKind.TypeParameter)
                {
                    // For named typed, namespaces, and type parameters (potentially constrainted to interface with statics), we flip things around.
                    // We only want statics and not instance members.
                    excludeInstance = true;
                    excludeStatic = false;
                    containerSymbol = symbol;
                }

                // Special case parameters. If we have a normal (non this/base) parameter, then that's what we want to
                // lookup symbols off of as we have a lot of special logic for determining member symbols of lambda
                // parameters.
                //
                // If it is a this/base parameter and we're in a static context, we shouldn't show anything
                if (symbol is IParameterSymbol parameter)
                {
                    if (parameter.IsThis && expression.IsInStaticContext())
                        return default;

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
                return default;

            // We don't provide any member from System.Void (which is valid only in the context of typeof operation).
            // Try to bail early to avoid unnecessary work even though compiler will handle this case for us.
            if (containerSymbol is INamedTypeSymbol typeSymbol && typeSymbol.IsSystemVoid())
                return default;

            Debug.Assert(!excludeInstance || !excludeStatic);

            // nameof(X.|
            // Show static and instance members.
            // Show base methods for "ref struct"s
            if (_context.IsNameOfContext)
            {
                excludeInstance = false;
                excludeStatic = false;
                excludeBaseMethodsForRefStructs = false;
            }

            var useBaseReferenceAccessibility = symbol is IParameterSymbol { IsThis: true } p && !p.Type.Equals(containerType);
            var symbols = GetMemberSymbols(containerSymbol, position: originalExpression.SpanStart, excludeInstance, useBaseReferenceAccessibility, unwrapNullable, isForDereference);

            // If we're showing instance members, don't include nested types
            var namedSymbols = excludeStatic
                ? symbols.WhereAsArray(s => !(s.IsStatic || s is ITypeSymbol))
                : symbols;

            //If container type is "ref struct" then we should exclude methods from object and ValueType that are not overriden
            //if recomendations are requested not in nameof context,
            //because calling them produces a compiler error due to unallowed boxing. See https://github.com/dotnet/roslyn/issues/35178
            if (excludeBaseMethodsForRefStructs && containerType is not null && containerType.IsRefLikeType)
            {
                namedSymbols = namedSymbols.RemoveAll(s => s.ContainingType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType);
            }

            // if we're dotting off an instance, then add potential operators/indexers/conversions that may be
            // applicable to it as well.
            var unnamedSymbols = _context.IsNameOfContext || excludeInstance
                ? default
                : GetUnnamedSymbols(originalExpression);
            return new RecommendedSymbols(namedSymbols, unnamedSymbols);
        }

        private ImmutableArray<ISymbol> GetUnnamedSymbols(ExpressionSyntax originalExpression)
        {
            var semanticModel = _context.SemanticModel;
            var container = GetContainerForUnnamedSymbols(semanticModel, originalExpression);
            if (container == null)
                return ImmutableArray<ISymbol>.Empty;

            // In a case like `x?.Y` if we bind the type of `.Y` we will get a value type back (like `int`), and not
            // `int?`.  However, we want to think of the constructed type as that's the type of the overall expression
            // that will be casted.
            if (originalExpression.GetRootConditionalAccessExpression() != null)
                container = TryMakeNullable(semanticModel.Compilation, container);

            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var symbols);

            AddIndexers(container, symbols);
            AddOperators(container, symbols);
            AddConversions(container, symbols);

            return symbols.ToImmutable();
        }

        private ITypeSymbol? GetContainerForUnnamedSymbols(SemanticModel semanticModel, ExpressionSyntax originalExpression)
        {
            return originalExpression.ShouldNameExpressionBeTreatedAsExpressionInsteadOfType(_context.SemanticModel, out _, out var container)
                ? container
                : semanticModel.GetTypeInfo(originalExpression, _cancellationToken).Type;
        }

        private void AddIndexers(ITypeSymbol container, ArrayBuilder<ISymbol> symbols)
        {
            var containingType = _context.SemanticModel.GetEnclosingNamedType(_context.Position, _cancellationToken);
            if (containingType == null)
                return;

            foreach (var member in container.RemoveNullableIfPresent().GetAccessibleMembersInThisAndBaseTypes<IPropertySymbol>(containingType))
            {
                if (member.IsIndexer)
                    symbols.Add(member);
            }
        }
    }
}
