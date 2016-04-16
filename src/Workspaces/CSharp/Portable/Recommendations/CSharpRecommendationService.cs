// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    [ExportLanguageService(typeof(IRecommendationService), LanguageNames.CSharp), Shared]
    internal class CSharpRecommendationService : AbstractRecommendationService
    {
        protected override Task<Tuple<IEnumerable<ISymbol>, AbstractSyntaxContext>> GetRecommendedSymbolsAtPositionWorkerAsync(
            Workspace workspace, SemanticModel semanticModel, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var context = CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken);

            var filterOutOfScopeLocals = options.GetOption(RecommendationOptions.FilterOutOfScopeLocals, semanticModel.Language);
            var symbols = GetSymbolsWorker(context, filterOutOfScopeLocals, cancellationToken);

            var hideAdvancedMembers = options.GetOption(RecommendationOptions.HideAdvancedMembers, semanticModel.Language);
            symbols = symbols.FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, semanticModel.Compilation);

            return Task.FromResult(Tuple.Create<IEnumerable<ISymbol>, AbstractSyntaxContext>(symbols, context));
        }

        private static IEnumerable<ISymbol> GetSymbolsWorker(
            CSharpSyntaxContext context,
            bool filterOutOfScopeLocals,
            CancellationToken cancellationToken)
        {
            if (context.IsInNonUserCode ||
                context.IsPreProcessorDirectiveContext)
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            // TODO: don't show completion set at namespace name part to match Dev10 behavior
            // if we want to provide new feature that shows all existing namespaces later, remove this
            if (context.IsNamespaceDeclarationNameContext)
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            if (context.IsRightOfNameSeparator)
            {
                return GetSymbolsOffOfContainer(context, cancellationToken);
            }
            else
            {
                return GetSymbolsForCurrentContext(context, filterOutOfScopeLocals, cancellationToken);
            }
        }

        private static IEnumerable<ISymbol> GetSymbolsForCurrentContext(
            CSharpSyntaxContext context,
            bool filterOutOfScopeLocals,
            CancellationToken cancellationToken)
        {
            if (context.IsGlobalStatementContext)
            {
                // Script and interactive
                return GetSymbolsForGlobalStatementContext(context, cancellationToken);
            }
            else if (context.IsAnyExpressionContext ||
                     context.IsStatementContext ||
                     context.SyntaxTree.IsDefiniteCastTypeContext(context.Position, context.LeftToken, cancellationToken))
            {
                // GitHub #717: With automatic brace completion active, typing '(i' produces "(i)", which gets parsed as
                // as cast. The user might be trying to type a parenthesized expression, so even though a cast
                // is a type-only context, we'll show all symbols anyway.
                return GetSymbolsForExpressionOrStatementContext(context, filterOutOfScopeLocals, cancellationToken);
            }
            else if (context.IsTypeContext || context.IsNamespaceContext)
            {
                return GetSymbolsForTypeOrNamespaceContext(context, cancellationToken);
            }
            else if (context.IsLabelContext)
            {
                return GetSymbolsForLabelContext(context, cancellationToken);
            }
            else if (context.IsTypeArgumentOfConstraintContext)
            {
                return GetSymbolsForTypeArgumentOfConstraintClause(context, cancellationToken);
            }
            else if (context.IsDestructorTypeContext)
            {
                return SpecializedCollections.SingletonEnumerable(context.SemanticModel.GetDeclaredSymbol(context.ContainingTypeOrEnumDeclaration, cancellationToken));
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        private static IEnumerable<ISymbol> GetSymbolsOffOfContainer(
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            // Ensure that we have the correct token in A.B| case
            var node = context.TargetToken.Parent;

            if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                return GetSymbolsOffOfExpression(context, ((MemberAccessExpressionSyntax)node).Expression, cancellationToken);
            }
            else if (node.Kind() == SyntaxKind.PointerMemberAccessExpression)
            {
                return GetSymbolsOffOfDereferencedExpression(context, ((MemberAccessExpressionSyntax)node).Expression, cancellationToken);
            }
            else if (node.Kind() == SyntaxKind.QualifiedName)
            {
                return GetSymbolsOffOfName(context, ((QualifiedNameSyntax)node).Left, cancellationToken);
            }
            else if (node.Kind() == SyntaxKind.AliasQualifiedName)
            {
                return GetSymbolsOffOffAlias(context, ((AliasQualifiedNameSyntax)node).Alias, cancellationToken);
            }
            else if (node.Kind() == SyntaxKind.MemberBindingExpression)
            {
                var parentConditionalAccess = node.GetParentConditionalAccessExpression();
                return GetSymbolsOffOfConditionalReceiver(context, parentConditionalAccess.Expression, cancellationToken);
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }
        }

        private static IEnumerable<ISymbol> GetSymbolsForGlobalStatementContext(
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            var position = context.Position;
            var token = context.LeftToken;

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
                var compUnit = (CompilationUnitSyntax)syntaxTree.GetRoot(cancellationToken);
                if (compUnit.Usings.Count > 0 && compUnit.Usings.Last().GetLastToken() == token)
                {
                    token = token.GetNextToken(includeZeroWidth: true);
                }
            }

            var symbols = context.SemanticModel
                .LookupSymbols(token.SpanStart);

            return symbols;
        }

        private static IEnumerable<ISymbol> GetSymbolsForTypeArgumentOfConstraintClause(
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var enclosingSymbol = context.LeftToken.Parent
                .AncestorsAndSelf()
                .Select(n => context.SemanticModel.GetDeclaredSymbol(n, cancellationToken))
                .WhereNotNull()
                .FirstOrDefault();

            var symbols = enclosingSymbol != null
                ? enclosingSymbol.GetTypeArguments()
                : SpecializedCollections.EmptyEnumerable<ISymbol>();

            return symbols;
        }

        private static IEnumerable<ISymbol> GetSymbolsOffOffAlias(
            CSharpSyntaxContext context,
            IdentifierNameSyntax alias,
            CancellationToken cancellationToken)
        {
            var aliasSymbol = context.SemanticModel.GetAliasInfo(alias, cancellationToken);
            if (aliasSymbol == null)
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            return context.SemanticModel.LookupNamespacesAndTypes(
                alias.SpanStart,
                aliasSymbol.Target);
        }

        private static IEnumerable<ISymbol> GetSymbolsForLabelContext(
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var allLabels = context.SemanticModel.LookupLabels(context.LeftToken.SpanStart);

            // Exclude labels (other than 'default') that come from case switch statements

            return allLabels
                .Where(label => label.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken)
                    .IsKind(SyntaxKind.LabeledStatement, SyntaxKind.DefaultSwitchLabel))
                .AsImmutableOrEmpty();
        }

        private static IEnumerable<ISymbol> GetSymbolsForTypeOrNamespaceContext(
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var symbols = context.SemanticModel.LookupNamespacesAndTypes(context.LeftToken.SpanStart);

            if (context.TargetToken.IsUsingKeywordInUsingDirective())
            {
                return symbols.Where(s => s.IsNamespace());
            }

            if (context.TargetToken.IsStaticKeywordInUsingDirective())
            {
                return symbols.Where(s => !s.IsDelegateType() && !s.IsInterfaceType());
            }

            return symbols;
        }

        private static IEnumerable<ISymbol> GetSymbolsForExpressionOrStatementContext(
            CSharpSyntaxContext context,
            bool filterOutOfScopeLocals,
            CancellationToken cancellationToken)
        {
            // Check if we're in an interesting situation like this:
            //
            //     i          // <-- here
            //     I = 0;

            // The problem is that "i I = 0" causes a local to be in scope called "I".  So, later when
            // we look up symbols, it masks any other 'I's in scope (i.e. if there's a field with that 
            // name).  If this is the case, we do not want to filter out inaccessible locals.
            if (filterOutOfScopeLocals)
            {
                if (context.LeftToken.Parent.IsFoundUnder<LocalDeclarationStatementSyntax>(d => d.Declaration.Type))
                {
                    filterOutOfScopeLocals = false;
                }
            }

            IEnumerable<ISymbol> symbols = !context.IsNameOfContext && context.LeftToken.Parent.IsInStaticContext()
                ? context.SemanticModel.LookupStaticMembers(context.LeftToken.SpanStart)
                : context.SemanticModel.LookupSymbols(context.LeftToken.SpanStart);

            // Filter out any extension methods that might be imported by a using static directive.
            // But include extension methods declared in the context's type or it's parents
            var contextEnclosingNamedType = context.SemanticModel.GetEnclosingNamedType(context.Position, cancellationToken);
            var contextOuterTypes = context.GetOuterTypes(cancellationToken);
            symbols = symbols.Where(symbol => !symbol.IsExtensionMethod() ||
                                              contextEnclosingNamedType.Equals(symbol.ContainingType) ||
                                              contextOuterTypes.Any(outerType => outerType.Equals(symbol.ContainingType)));

            // The symbols may include local variables that are declared later in the method and
            // should not be included in the completion list, so remove those. Filter them away,
            // unless we're in the debugger, where we show all locals in scope.
            if (filterOutOfScopeLocals)
            {
                symbols = symbols.Where(symbol => !symbol.IsInaccessibleLocal(context.Position));
            }

            return symbols;
        }

        private static IEnumerable<ISymbol> GetSymbolsOffOfName(
            CSharpSyntaxContext context,
            NameSyntax name,
            CancellationToken cancellationToken)
        {
            // Check if we're in an interesting situation like this:
            //
            //     int i = 5;
            //     i.          // <-- here
            //     List<string> ml = new List<string>();

            // The problem is that "i.List<string>" gets parsed as a type.  In this case we need to
            // try binding again as if "i" is an expression and not a type.  In order to do that, we
            // need to speculate as to what 'i' meant if it wasn't part of a local declaration's
            // type.

            if (name.IsFoundUnder<LocalDeclarationStatementSyntax>(d => d.Declaration.Type))
            {
                var speculativeBinding = context.SemanticModel.GetSpeculativeSymbolInfo(name.SpanStart, name, SpeculativeBindingOption.BindAsExpression);
                var container = context.SemanticModel.GetSpeculativeTypeInfo(name.SpanStart, name, SpeculativeBindingOption.BindAsExpression).Type;
                return GetSymbolsOffOfBoundExpression(context, name, name, speculativeBinding, container, cancellationToken);
            }

            // We're in a name-only context, since if we were an expression we'd be a
            // MemberAccessExpressionSyntax. Thus, let's do other namespaces and types.
            var nameBinding = context.SemanticModel.GetSymbolInfo(name, cancellationToken);

            var symbol = nameBinding.Symbol as INamespaceOrTypeSymbol;
            if (symbol != null)
            {
                if (context.IsNameOfContext)
                {
                    return context.SemanticModel.LookupSymbols(position: name.SpanStart, container: symbol);
                }

                IEnumerable<ISymbol> symbols = context.SemanticModel.LookupNamespacesAndTypes(
                    position: name.SpanStart,
                    container: symbol);

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
                    if (usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                    {
                        return symbols.Where(s => !s.IsDelegateType() && !s.IsInterfaceType());
                    }
                    else
                    {
                        symbols = symbols.Where(s => s.IsNamespace()).ToList();
                    }
                }

                if (symbols.Any())
                {
                    return symbols;
                }
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        private static IEnumerable<ISymbol> GetSymbolsOffOfExpression(
            CSharpSyntaxContext context,
            ExpressionSyntax originalExpression,
            CancellationToken cancellationToken)
        {
            var expression = originalExpression.WalkDownParentheses();
            var leftHandBinding = context.SemanticModel.GetSymbolInfo(expression, cancellationToken);
            var container = context.SemanticModel.GetTypeInfo(expression, cancellationToken).Type;

            var normalSymbols = GetSymbolsOffOfBoundExpression(context, originalExpression, expression, leftHandBinding, container, cancellationToken);

            // Check for the Color Color case.
            if (originalExpression.CanAccessInstanceAndStaticMembersOffOf(context.SemanticModel, cancellationToken))
            {
                var speculativeSymbolInfo = context.SemanticModel.GetSpeculativeSymbolInfo(expression.SpanStart, expression, SpeculativeBindingOption.BindAsTypeOrNamespace);

                var typeMembers = GetSymbolsOffOfBoundExpression(context, originalExpression, expression, speculativeSymbolInfo, container, cancellationToken);

                normalSymbols = normalSymbols.Concat(typeMembers);
            }

            return normalSymbols;
        }

        private static IEnumerable<ISymbol> GetSymbolsOffOfDereferencedExpression(
            CSharpSyntaxContext context,
            ExpressionSyntax originalExpression,
            CancellationToken cancellationToken)
        {
            var expression = originalExpression.WalkDownParentheses();
            var leftHandBinding = context.SemanticModel.GetSymbolInfo(expression, cancellationToken);

            var container = context.SemanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (container is IPointerTypeSymbol)
            {
                container = ((IPointerTypeSymbol)container).PointedAtType;
            }

            return GetSymbolsOffOfBoundExpression(context, originalExpression, expression, leftHandBinding, container, cancellationToken);
        }

        private static IEnumerable<ISymbol> GetSymbolsOffOfConditionalReceiver(
            CSharpSyntaxContext context,
            ExpressionSyntax originalExpression,
            CancellationToken cancellationToken)
        {
            // Given ((T?)t)?.|, the '.' will behave as if the expression was actually ((T)t).|. More plainly,
            // a member access off of a conditional receiver of nullable type binds to the unwrapped nullable
            // type. This is not exposed via the binding information for the LHS, so repeat this work here.

            var expression = originalExpression.WalkDownParentheses();
            var leftHandBinding = context.SemanticModel.GetSymbolInfo(expression, cancellationToken);
            var container = context.SemanticModel.GetTypeInfo(expression, cancellationToken).Type.RemoveNullableIfPresent();

            // If the thing on the left is a type, namespace, or alias, we shouldn't show anything in
            // IntelliSense.
            if (leftHandBinding.GetBestOrAllSymbols().FirstOrDefault().MatchesKind(SymbolKind.NamedType, SymbolKind.Namespace, SymbolKind.Alias))
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            return GetSymbolsOffOfBoundExpression(context, originalExpression, expression, leftHandBinding, container, cancellationToken);
        }

        private static IEnumerable<ISymbol> GetSymbolsOffOfBoundExpression(
            CSharpSyntaxContext context,
            ExpressionSyntax originalExpression,
            ExpressionSyntax expression,
            SymbolInfo leftHandBinding,
            INamespaceOrTypeSymbol container,
            CancellationToken cancellationToken)
        {
            var useBaseReferenceAccessibility = false;
            var excludeInstance = false;
            var excludeStatic = false;
            var symbol = leftHandBinding.GetBestOrAllSymbols().FirstOrDefault();

            if (symbol != null)
            {
                // If the thing on the left is a type, namespace or alias and the original
                // expression was parenthesized, we shouldn't show anything in IntelliSense.
                if (originalExpression.IsKind(SyntaxKind.ParenthesizedExpression) &&
                    symbol.MatchesKind(SymbolKind.NamedType,
                                       SymbolKind.Namespace,
                                       SymbolKind.Alias))
                {
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
                }

                // If the thing on the left is a lambda expression, we shouldn't show anything.
                if (symbol.Kind == SymbolKind.Method &&
                    ((IMethodSymbol)symbol).MethodKind == MethodKind.AnonymousFunction)
                {
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
                }

                // If the thing on the left is an event that can't be used as a field, we shouldn't show anything
                if (symbol.Kind == SymbolKind.Event &&
                    !context.SemanticModel.IsEventUsableAsField(originalExpression.SpanStart, (IEventSymbol)symbol))
                {
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
                }

                // If the thing on the left is a this parameter (e.g. this or base) and we're in a static context,
                // we shouldn't show anything
                if (symbol.IsThisParameter() &&
                    expression.IsInStaticContext())
                {
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
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
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            Debug.Assert(!excludeInstance || !excludeStatic);
            Debug.Assert(!excludeInstance || !useBaseReferenceAccessibility);

            // nameof(X.|
            // Show static and instance members.
            if (context.IsNameOfContext)
            {
                excludeInstance = false;
                excludeStatic = false;
            }

            var position = originalExpression.SpanStart;

            IEnumerable<ISymbol> symbols = useBaseReferenceAccessibility
                ? context.SemanticModel.LookupBaseMembers(position)
                : excludeInstance
                    ? context.SemanticModel.LookupStaticMembers(position, container)
                    : context.SemanticModel.LookupSymbols(position, container, includeReducedExtensionMethods: true);

            // If we're showing instance members, don't include nested types
            return excludeStatic
                ? symbols.Where(s => !s.IsStatic && !(s is ITypeSymbol))
                : symbols;
        }
    }
}
