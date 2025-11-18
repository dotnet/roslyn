// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations;

internal partial class CSharpRecommendationService
{
    private sealed partial class CSharpRecommendationServiceRunner(
        CSharpSyntaxContext context, bool filterOutOfScopeLocals, CancellationToken cancellationToken)
        : AbstractRecommendationServiceRunner(context, filterOutOfScopeLocals, cancellationToken)
    {
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
            if (_context.IsInNonUserCode || _context.IsPreProcessorDirectiveContext)
                return default;

            if (_context.IsRightOfNameSeparator)
                return GetSymbolsOffOfContainer();

            return new RecommendedSymbols(GetSymbolsForCurrentContext());
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
                return symbol == null ? [] : [symbol];
            }
            else if (_context.IsNamespaceDeclarationNameContext)
            {
                return GetSymbolsForNamespaceDeclarationNameContext<BaseNamespaceDeclarationSyntax>();
            }
            else if (_context.IsEnumBaseListContext)
            {
                return GetSymbolsForEnumBaseList(container: null);
            }

            return [];
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

            if (IsConstantPatternContainerContext())
            {
                // We are building a pattern expression, and thus we can only access either constants, types or namespaces.
                return node switch
                {
                    // x is (A.$$
                    // x switch { A.$$
                    // x switch { { Property: A.$$
                    MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression) memberAccess
                        => GetSymbolsOffOfExpressionInConstantPattern(memberAccess.Expression),
                    // x is A.$$
                    QualifiedNameSyntax qualifiedName => GetSymbolsOffOfExpressionInConstantPattern(qualifiedName.Left),
                    _ => default,
                };
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
                AliasQualifiedNameSyntax aliasName => GetSymbolsOffOfAlias(aliasName.Alias),
                MemberBindingExpressionSyntax => GetSymbolsOffOfConditionalReceiver(node.GetParentConditionalAccessExpression()!.Expression),
                _ => default,
            };

            bool IsConstantPatternContainerContext()
            {
                if (node is MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression))
                {
                    for (var current = node; current != null; current = current.Parent)
                    {
                        if (current.Kind() == SyntaxKind.ConstantPattern)
                            return true;

                        if (current.Kind() == SyntaxKind.ParenthesizedExpression)
                            continue;

                        if (current.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                            continue;

                        break;
                    }
                }
                else if (node is QualifiedNameSyntax)
                {
                    var last = node;
                    for (var current = node; current != null; last = current, current = current.Parent)
                    {
                        if (current is BinaryExpressionSyntax(SyntaxKind.IsExpression) binaryExpression &&
                            binaryExpression.Right == last)
                        {
                            return true;
                        }

                        if (current.Kind() == SyntaxKind.QualifiedName)
                            continue;

                        if (current.Kind() == SyntaxKind.AliasQualifiedName)
                            continue;

                        break;
                    }
                }

                return false;
            }
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
                : [];

            return ImmutableArray<ISymbol>.CastUp(symbols);
        }

        private RecommendedSymbols GetSymbolsOffOfAlias(IdentifierNameSyntax alias)
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
            var semanticModel = _context.SemanticModel;
            var symbols = semanticModel.LookupNamespacesAndTypes(_context.LeftToken.SpanStart);

            if (_context.TargetToken.IsUsingKeywordInUsingDirective())
                return symbols.WhereAsArray(static s => s is INamespaceSymbol);

            if (_context.TargetToken.IsStaticKeywordContextInUsingDirective())
                return symbols.WhereAsArray(static s => !s.IsDelegateType());

            if (_context.IsBaseListContext)
            {
                // Filter out the type we're in the inheritance list for if it has no nested types.  A type can't show
                // up in its own inheritance list (unless being used to 
                //
                // Note: IsBaseListContext requires that we have a type declaration ancestor above us..
                var containingType = semanticModel.GetRequiredDeclaredSymbol(
                    _context.TargetToken.GetRequiredAncestor<TypeDeclarationSyntax>(), _cancellationToken).OriginalDefinition;
                if (containingType.GetTypeMembers().IsEmpty)
                    return symbols.WhereAsArray(static (s, containingType) => !Equals(s.OriginalDefinition, containingType), containingType);
            }

            return symbols;
        }

        private ImmutableArray<ISymbol> GetSymbolsForExpressionOrStatementContext()
        {
            var contextNode = _context.LeftToken.GetRequiredParent();
            var semanticModel = _context.SemanticModel;

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
                filterOutOfScopeLocals =
                    !contextNode.IsFoundUnder<LocalDeclarationStatementSyntax>(d => d.Declaration.Type) &&
                    !contextNode.IsFoundUnder<DeclarationExpressionSyntax>(d => d.Type);
            }

            ImmutableArray<ISymbol> symbols;
            if (_context.IsNameOfContext)
            {
                symbols = semanticModel.LookupSymbols(_context.LeftToken.SpanStart);

                // We may be inside of a nameof() on a method.  In that case, we want to include the parameters in
                // the nameof if LookupSymbols didn't already return them.
                var enclosingMethodOrLambdaNode = contextNode.AncestorsAndSelf().FirstOrDefault(n => n is AnonymousFunctionExpressionSyntax or BaseMethodDeclarationSyntax);
                var enclosingMethodOrLambda = enclosingMethodOrLambdaNode is null
                    ? null
                    : semanticModel.GetSymbolInfo(enclosingMethodOrLambdaNode).GetAnySymbol() ?? semanticModel.GetDeclaredSymbol(enclosingMethodOrLambdaNode);
                if (enclosingMethodOrLambda is IMethodSymbol method)
                    symbols = [.. symbols, .. method.Parameters];
            }
            else
            {
                symbols = contextNode.IsInStaticContext()
                    ? semanticModel.LookupStaticMembers(_context.LeftToken.SpanStart)
                    : semanticModel.LookupSymbols(_context.LeftToken.SpanStart);

                symbols = FilterOutUncapturableParameters(symbols, contextNode);
            }

            // Filter out any extension methods that might be imported by a using static directive.
            // But include extension methods declared in the context's type or it's parents
            var contextOuterTypes = ComputeOuterTypes(_context, _cancellationToken);
            var contextEnclosingNamedType = semanticModel.GetEnclosingNamedType(_context.Position, _cancellationToken);

            return symbols.Distinct().WhereAsArray(
                static (symbol, args) => !IsUndesirable(args._context, args.contextEnclosingNamedType, args.contextOuterTypes, args.filterOutOfScopeLocals, symbol, args._cancellationToken),
                (_context, contextOuterTypes, contextEnclosingNamedType, filterOutOfScopeLocals, _cancellationToken));

            static bool IsUndesirable(
                CSharpSyntaxContext context,
                INamedTypeSymbol? enclosingNamedType,
                ISet<INamedTypeSymbol> outerTypes,
                bool filterOutOfScopeLocals,
                ISymbol symbol,
                CancellationToken cancellationToken)
            {
                // filter our top level locals if we're inside a type declaration.
                if (context.ContainingTypeDeclaration != null && symbol.ContainingSymbol.Name == WellKnownMemberNames.TopLevelStatementsEntryPointMethodName)
                    return true;

                if (symbol.IsExtensionMethod() &&
                    !Equals(enclosingNamedType, symbol.ContainingType) &&
                    !outerTypes.Contains(symbol.ContainingType))
                {
                    return true;
                }

                // The symbols may include local variables that are declared later in the method and should not be
                // included in the completion list, so remove those. Filter them away, unless we're in the debugger,
                // where we show all locals in scope.
                if (filterOutOfScopeLocals && symbol.IsInaccessibleLocal(context.Position))
                    return true;

                // Outside of a nameof(...) we don't want to include a primary constructor parameter if it's not
                // available.  Inside of a nameof(...) we do want to include it as it's always legal and causes no
                // warnings.
                if (!context.IsNameOfContext &&
                    symbol is IParameterSymbol parameterSymbol &&
                    parameterSymbol.IsPrimaryConstructor(cancellationToken))
                {
                    // Primary constructor parameters are only available in instance members, so filter out if we're in
                    // a static context.
                    if (!context.IsInstanceContext)
                        return true;

                    // If the parameter was already captured by a field, or by passing to a base-class constructor, then
                    // we don't want to offer it as the user will get a warning about double storage by capturing both
                    // into the field/base-type, and synthesized storage for the parameter.
                    if (IsCapturedPrimaryConstructorParameter(context, enclosingNamedType, parameterSymbol, cancellationToken))
                        return true;
                }

                return false;
            }

            static bool IsCapturedPrimaryConstructorParameter(
                CSharpSyntaxContext context,
                INamedTypeSymbol? enclosingNamedType,
                IParameterSymbol parameterSymbol,
                CancellationToken cancellationToken)
            {
                // Fine to offer primary constructor parameters in field/property initializers 
                var initializer = context.TargetToken.GetAncestor<EqualsValueClauseSyntax>();
                if (initializer is
                    {
                        Parent: PropertyDeclarationSyntax or
                                VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax } }
                    })
                {
                    return false;
                }

                // Also fine to offer primary constructor parameters in the base type list of that type.
                var baseTypeSyntax = context.TargetToken.GetAncestor<PrimaryConstructorBaseTypeSyntax>();
                if (baseTypeSyntax != null)
                    return false;

                // We're not in an initializer.  Filter out this primary constructor parameter if it's already been
                // captured by an existing field or property initializer.

                if (enclosingNamedType != null)
                {
                    var parameterName = parameterSymbol.Name;
                    foreach (var reference in enclosingNamedType.DeclaringSyntaxReferences)
                    {
                        if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax typeDeclaration)
                            continue;

                        // See if the parameter was captured into a base-type constructor through the base list.
                        if (typeDeclaration.BaseList != null)
                        {
                            foreach (var baseType in typeDeclaration.BaseList.Types)
                            {
                                if (baseType is PrimaryConstructorBaseTypeSyntax primaryConstructorBase)
                                {
                                    foreach (var argument in primaryConstructorBase.ArgumentList.Arguments)
                                    {
                                        if (argument.Expression is IdentifierNameSyntax { Identifier.ValueText: var argumentIdentifier } &&
                                            argumentIdentifier == parameterName)
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }

                        // Next, see if any field or property in the type captures the primary constructor parameter in its initializer.
                        foreach (var member in typeDeclaration.Members)
                        {
                            if (member is FieldDeclarationSyntax fieldDeclaration)
                            {
                                foreach (var variableDeclarator in fieldDeclaration.Declaration.Variables)
                                {
                                    if (variableDeclarator.Initializer?.Value is IdentifierNameSyntax { Identifier.ValueText: var fieldInitializerIdentifier } &&
                                        fieldInitializerIdentifier == parameterName)
                                    {
                                        return true;
                                    }
                                }
                            }
                            else if (member is PropertyDeclarationSyntax { Initializer.Value: IdentifierNameSyntax { Identifier.ValueText: var propertyInitializerIdentifier } } &&
                                     propertyInitializerIdentifier == parameterName)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        private static ImmutableArray<ISymbol> FilterOutUncapturableParameters(ImmutableArray<ISymbol> symbols, SyntaxNode contextNode)
        {
            // Can't capture parameters across a static lambda/local function

            var containingStaticFunction = contextNode.FirstAncestorOrSelf<SyntaxNode>(a => a switch
            {
                AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.Modifiers.Any(SyntaxKind.StaticKeyword),
                LocalFunctionStatementSyntax localFunction => localFunction.Modifiers.Any(SyntaxKind.StaticKeyword),
                _ => false,
            });

            if (containingStaticFunction is null)
                return symbols;

            return symbols.WhereAsArray(s =>
            {
                if (s is not IParameterSymbol { DeclaringSyntaxReferences: [var parameterReference] })
                    return true;

                return parameterReference.Span.Start >= containingStaticFunction.SpanStart;
            });
        }

        private RecommendedSymbols GetSymbolsOffOfName(NameSyntax name)
        {
            // Using an is pattern on an enum is a qualified name, but normal symbol processing works fine
            if (_context.IsEnumTypeMemberAccessContext)
                return GetSymbolsOffOfExpression(name);

            if (name.ShouldNameExpressionBeTreatedAsExpressionInsteadOfType(_context.SemanticModel, out var nameBinding, out var container))
                return GetSymbolsOffOfBoundExpression(name, name, nameBinding, container, unwrapNullable: false, isForDereference: false, allowColorColor: true);

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
                    : symbols.WhereAsArray(s => s is INamespaceSymbol));
            }

            return new RecommendedSymbols(symbols);
        }

        private RecommendedSymbols GetSymbolsOffOfExpressionInConstantPattern(ExpressionSyntax? originalExpression)
        {
            if (originalExpression is null)
                return default;

            var semanticModel = _context.SemanticModel;
            var boundSymbol = semanticModel.GetSymbolInfo(originalExpression, _cancellationToken);

            if (boundSymbol.Symbol is not INamespaceOrTypeSymbol namespaceOrType)
            {
                // Likely a Color Color case, so we reinterpret the bound symbol into a type
                if (originalExpression is IdentifierNameSyntax identifier)
                {
                    var reinterpretedBinding = semanticModel.GetSpeculativeSymbolInfo(identifier.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
                    var reinterpretedSymbol = reinterpretedBinding.GetAnySymbol();
                    var container = _context.SemanticModel.GetTypeInfo(identifier, _cancellationToken).Type;

                    // The reinterpretation must be a namespace or type, since we cannot have a
                    // constant expression out of dotting a constant value, like a x.Length
                    // If all we can bind to is a const local or const field, we cannot offer valid suggestions
                    if (reinterpretedSymbol is not INamespaceOrTypeSymbol)
                        return default;

                    var expression = originalExpression.WalkDownParentheses();

                    return GetSymbolsOffOfBoundExpressionWorker(
                        reinterpretedBinding,
                        originalExpression,
                        expression,
                        container,
                        unwrapNullable: false,
                        isForDereference: false);
                }

                return default;
            }

            var containingType = _context.SemanticModel.GetEnclosingNamedType(_context.Position, _cancellationToken);
            if (containingType == null)
                return default;

            // A constant pattern may only include qualifications to
            // - namespaces (from other namespaces or aliases),
            // - types (from aliases, namespaces or other types),
            // - constant fields (from types)
            // Methods, properties, events, non-constant fields etc. are excluded since they are not constant expressions
            var symbols = namespaceOrType
                .GetMembers()
                .WhereAsArray(symbol => symbol is INamespaceOrTypeSymbol or IFieldSymbol { IsConst: true }
                    && symbol.IsAccessibleWithin(containingType));
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

            return GetSymbolsOffOfBoundExpression(
                originalExpression, expression, leftHandBinding, container, unwrapNullable: false, isForDereference: false, allowColorColor: true);
        }

        private RecommendedSymbols GetSymbolsOffOfDereferencedExpression(ExpressionSyntax originalExpression)
        {
            var expression = originalExpression.WalkDownParentheses();
            var leftHandBinding = _context.SemanticModel.GetSymbolInfo(expression, _cancellationToken);
            var container = _context.SemanticModel.GetTypeInfo(expression, _cancellationToken).Type;

            // Can't access statics through a pointer so do not allow for the `Color Color` case.
            return GetSymbolsOffOfBoundExpression(
                originalExpression, expression, leftHandBinding, container, unwrapNullable: false, isForDereference: true, allowColorColor: false);
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

            // Can't access statics through `?.` so do not allow for the `Color Color` case.
            return GetSymbolsOffOfBoundExpression(
                originalExpression, expression, leftHandBinding, container, unwrapNullable: true, isForDereference: false, allowColorColor: false);
        }

        private RecommendedSymbols GetSymbolsOffOfBoundExpression(
            ExpressionSyntax originalExpression,
            ExpressionSyntax expression,
            SymbolInfo leftHandBinding,
            ITypeSymbol? containerType,
            bool unwrapNullable,
            bool isForDereference,
            bool allowColorColor)
        {
            var result = GetSymbolsOffOfBoundExpressionWorker(leftHandBinding, originalExpression, expression, containerType, unwrapNullable, isForDereference);
            if (!allowColorColor || !CanAccessInstanceAndStaticMembersOffOf(out var reinterpretedBinding))
                return result;

            var typeMembers = GetSymbolsOffOfBoundExpressionWorker(reinterpretedBinding, originalExpression, expression, containerType, unwrapNullable, isForDereference);

            return new RecommendedSymbols(
                [.. result.NamedSymbols, .. typeMembers.NamedSymbols],
                result.UnnamedSymbols);

            bool CanAccessInstanceAndStaticMembersOffOf(out SymbolInfo reinterpretedBinding)
            {
                reinterpretedBinding = default;

                // Check for the Color Color case.
                //
                // color color: if you bind "A" and you get a symbol and the type of that symbol is
                // Q; and if you bind "A" *again* as a type and you get type Q, then both A.static
                // and A.instance are permitted
                if (expression is not IdentifierNameSyntax identifier)
                    return false;

                var semanticModel = _context.SemanticModel;
                var symbol = leftHandBinding.GetAnySymbol();

                // If the symbol is currently bound as a named type, try to bind it as an instance.  Conversely, if it's
                // bound as an instance, try to bind it as a named type.
                INamedTypeSymbol? instanceType, staticType;
                if (symbol is INamedTypeSymbol namedType)
                {
                    reinterpretedBinding = semanticModel.GetSpeculativeSymbolInfo(identifier.SpanStart, identifier, SpeculativeBindingOption.BindAsExpression);
                    var reinterpretedSymbol = reinterpretedBinding.GetAnySymbol();

                    // has to actually have reinterpreted to something that has an instance type.
                    if (reinterpretedSymbol is INamespaceOrTypeSymbol)
                        return false;

                    instanceType = reinterpretedSymbol.GetSymbolType() as INamedTypeSymbol;
                    staticType = namedType;
                }
                else
                {
                    reinterpretedBinding = semanticModel.GetSpeculativeSymbolInfo(identifier.SpanStart, identifier, SpeculativeBindingOption.BindAsTypeOrNamespace);
                    var reinterpretedSymbol = reinterpretedBinding.GetAnySymbol();

                    // Has to actually have reinterpreted to a named typed.
                    if (reinterpretedSymbol is not INamedTypeSymbol reinterprettedNamedType)
                        return false;

                    instanceType = symbol.GetSymbolType() as INamedTypeSymbol;
                    staticType = reinterprettedNamedType;
                }

                if (instanceType is null || staticType is null)
                    return false;

                return SymbolEquivalenceComparer.Instance.Equals(instanceType, staticType);
            }
        }

        private RecommendedSymbols GetSymbolsOffOfBoundExpressionWorker(SymbolInfo leftHandBinding, ExpressionSyntax originalExpression, ExpressionSyntax expression, ITypeSymbol? containerType, bool unwrapNullable, bool isForDereference)
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
                    // For named typed, namespaces, and type parameters (potentially constrained to interface with statics), we flip things around.
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

            var namedSymbols = symbols.WhereAsArray(
                static (s, a) => !IsUndesirable(s, a.containerType, a.excludeStatic, a.excludeInstance, a.excludeBaseMethodsForRefStructs),
                (containerType, excludeStatic, excludeInstance, excludeBaseMethodsForRefStructs, 3));

            // if we're dotting off an instance, then add potential operators/indexers/conversions that may be
            // applicable to it as well.
            var unnamedSymbols = _context.IsNameOfContext || excludeInstance
                ? default
                : GetUnnamedSymbols(originalExpression);

            return new RecommendedSymbols(namedSymbols, unnamedSymbols);

            static bool IsUndesirable(
                ISymbol symbol,
                ITypeSymbol? containerType,
                bool excludeStatic,
                bool excludeInstance,
                bool excludeBaseMethodsForRefStructs)
            {
                // If we're showing instance members, don't include nested types
                if (excludeStatic)
                {
                    if (symbol.IsStatic || symbol is ITypeSymbol)
                        return true;
                }

                // If container type is "ref struct" then we should exclude methods from object and ValueType that are not
                // overridden if recommendations are requested not in nameof context, because calling them produces a
                // compiler error due to unallowed boxing. See https://github.com/dotnet/roslyn/issues/35178
                if (excludeBaseMethodsForRefStructs &&
                    containerType is { IsRefLikeType: true } &&
                    symbol.ContainingType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
                {
                    return true;
                }

                // We're accessing virtual statics off of an type parameter.  We cannot access normal static this
                // way, so filter them out.
                if (excludeInstance && containerType is ITypeParameterSymbol && symbol.IsStatic)
                {
                    if (!(symbol.IsVirtual || symbol.IsAbstract))
                        return true;
                }

                return false;
            }
        }

        private ImmutableArray<ISymbol> GetUnnamedSymbols(ExpressionSyntax originalExpression)
        {
            var semanticModel = _context.SemanticModel;
            var container = GetContainerForUnnamedSymbols(semanticModel, originalExpression);
            if (container == null)
                return [];

            // In a case like `x?.Y` if we bind the type of `.Y` we will get a value type back (like `int`), and not
            // `int?`.  However, we want to think of the constructed type as that's the type of the overall expression
            // that will be casted.
            if (originalExpression.GetRootConditionalAccessExpression() != null)
                container = TryMakeNullable(semanticModel.Compilation, container);

            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var symbols);

            AddIndexers(container, symbols);
            AddOperators(container, symbols);
            AddConversions(container, symbols);

            return symbols.ToImmutableAndClear();
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
