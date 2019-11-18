// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Recommendations
{
    internal abstract class AbstractRecommendationServiceRunner<TSyntaxContext>
        where TSyntaxContext : SyntaxContext
    {
        protected readonly TSyntaxContext _context;
        protected readonly bool _filterOutOfScopeLocals;
        protected readonly CancellationToken _cancellationToken;

        public AbstractRecommendationServiceRunner(
            TSyntaxContext context,
            bool filterOutOfScopeLocals,
            CancellationToken cancellationToken)
        {
            _context = context;
            _filterOutOfScopeLocals = filterOutOfScopeLocals;
            _cancellationToken = cancellationToken;
        }

        public abstract ImmutableArray<ISymbol> GetSymbols();

        // This code is to help give intellisense in the following case: 
        // query.Include(a => a.SomeProperty).ThenInclude(a => a.
        // where there are more than one overloads of ThenInclude accepting different types of parameters.
        protected ImmutableArray<ISymbol> GetSymbols(IParameterSymbol parameter, int position)
        {
            // Starting from a. in the example, looking for a => a.
            if (!(parameter is
            {
                ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } containingMethod
            }))
            {
                return default;
            }

            // Cannot proceed without DeclaringSyntaxReferences.
            // We expect that there is a single DeclaringSyntaxReferences in the scenario.
            // If anything changes on the compiler side, the approach should be revised.
            if (containingMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return default;
            }

            var syntaxFactsService = _context.Workspace.Services.GetLanguageServices(_context.SemanticModel.Language).GetService<ISyntaxFactsService>();

            // Check that a => a. belongs to an invocation.
            // Find its' ordinal in the invocation, e.g. ThenInclude(a => a.Something, a=> a.
            var lambdaSyntax = containingMethod.DeclaringSyntaxReferences.Single().GetSyntax(_cancellationToken);
            if (!(syntaxFactsService.IsAnonymousFunction(lambdaSyntax) &&
                syntaxFactsService.IsArgument(lambdaSyntax.Parent) &&
                syntaxFactsService.IsInvocationExpression(lambdaSyntax.Parent.Parent.Parent)))
            {
                return default;
            }

            var invocationExpression = lambdaSyntax.Parent.Parent.Parent;
            var arguments = syntaxFactsService.GetArgumentsOfInvocationExpression(invocationExpression);
            var ordinalInInvocation = arguments.IndexOf(lambdaSyntax.Parent);

            var invocation = _context.SemanticModel.GetSymbolInfo(invocationExpression, _cancellationToken);
            var candidateSymbols = invocation.GetAllSymbols();

            // parameter.Ordinal is the ordinal within (a,b,c) => b.
            // For candidate symbols of (a,b,c) => b., get types of all possible b.
            var parameterTypeSymbols = GetTypeSymbols(candidateSymbols, ordinalInInvocation: ordinalInInvocation, ordinalInLambda: parameter.Ordinal);

            // For each type of b., return all suitable members.
            return parameterTypeSymbols
                .SelectMany(parameterTypeSymbol => GetSymbols(parameterTypeSymbol, position, excludeInstance: false, useBaseReferenceAccessibility: false))
                .ToImmutableArray();
        }

        /// <summary>
        /// Tries to get a type of its' <paramref name="ordinalInLambda"/> lambda parameter of <paramref name="ordinalInInvocation"/> argument for each candidate symbol.
        /// </summary>
        /// <param name="candidateSymbols">symbols corresponding to <see cref="Expression{Func}"/> or <see cref="Func{some_args, TResult}"/>
        /// Here, some_args can be multi-variables lambdas as well, e.g. f((a,b) => a+b, (a,b,c)=>a*b*c.Length)
        /// </param>
        /// <param name="ordinalInInvocation">ordinal of the arguments of function: (a,b) or (a,b,c) in the example above</param>
        /// <param name="ordinalInLambda">ordinal of the lambda parameters, e.g. a, b or c.</param>
        /// <returns></returns>
        private ImmutableArray<ITypeSymbol> GetTypeSymbols(ImmutableArray<ISymbol> candidateSymbols, int ordinalInInvocation, int ordinalInLambda)
        {
            var expressionSymbol = _context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Expression<>).FullName);

            var builder = ArrayBuilder<ITypeSymbol>.GetInstance();

            foreach (var candidateSymbol in candidateSymbols)
            {
                if (candidateSymbol is IMethodSymbol method)
                {
                    ITypeSymbol type;
                    if (method.IsParams() && (ordinalInInvocation >= method.Parameters.Length - 1))
                    {
                        if (method.Parameters.LastOrDefault()?.Type is IArrayTypeSymbol arrayType)
                        {
                            type = arrayType.ElementType;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (ordinalInInvocation < method.Parameters.Length)
                    {
                        type = method.Parameters[ordinalInInvocation].Type;
                    }
                    else
                    {
                        continue;
                    }

                    // If type is <see cref="Expression{TDelegate}"/>, ignore <see cref="Expression"/> and use TDelegate.
                    // Ignore this check if expressionSymbol is null, e.g. semantic model is broken or incomplete or if the framework does not contain <see cref="Expression"/>.
                    if (expressionSymbol != null &&
                        type is INamedTypeSymbol expressionSymbolNamedTypeCandidate &&
                        expressionSymbolNamedTypeCandidate.OriginalDefinition.Equals(expressionSymbol))
                    {
                        var allTypeArguments = type.GetAllTypeArguments();
                        if (allTypeArguments.Length != 1)
                        {
                            continue;
                        }

                        type = allTypeArguments[0];
                    }

                    if (type.IsDelegateType())
                    {
                        var methods = type.GetMembers(WellKnownMemberNames.DelegateInvokeName);
                        if (methods.Length != 1)
                        {
                            continue;
                        }

                        var parameters = methods[0].GetParameters();
                        if (parameters.Length <= ordinalInLambda)
                        {
                            continue;
                        }

                        type = parameters[ordinalInLambda].Type;
                    }

                    builder.Add(type);
                }
            }

            return builder.ToImmutableAndFree().Distinct();
        }

        protected ImmutableArray<ISymbol> GetSymbolsForNamespaceDeclarationNameContext<TNamespaceDeclarationSyntax>()
            where TNamespaceDeclarationSyntax : SyntaxNode
        {
            var declarationSyntax = _context.TargetToken.GetAncestor<TNamespaceDeclarationSyntax>();

            if (declarationSyntax == null)
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            var semanticModel = _context.SemanticModel;
            var containingNamespaceSymbol = semanticModel.Compilation.GetCompilationNamespace(
                semanticModel.GetEnclosingNamespace(declarationSyntax.SpanStart, _cancellationToken));

            var symbols = semanticModel.LookupNamespacesAndTypes(declarationSyntax.SpanStart, containingNamespaceSymbol)
                                       .WhereAsArray(recommendationSymbol => IsNonIntersectingNamespace(recommendationSymbol, declarationSyntax));

            return symbols;
        }

        protected static bool IsNonIntersectingNamespace(ISymbol recommendationSymbol, SyntaxNode declarationSyntax)
        {
            //
            // Apart from filtering out non-namespace symbols, this also filters out the symbol
            // currently being declared. For example...
            //
            //     namespace X$$
            //
            // ...X won't show in the completion list (unless it is also declared elsewhere).
            //
            // In addition, in VB, it will filter out Bar from the sample below...
            //
            //     Namespace Goo.$$
            //         Namespace Bar
            //         End Namespace
            //     End Namespace
            //
            // ...unless, again, it's also declared elsewhere.
            //
            return recommendationSymbol.IsNamespace() &&
                   recommendationSymbol.Locations.Any(
                       candidateLocation => !(declarationSyntax.SyntaxTree == candidateLocation.SourceTree &&
                                              declarationSyntax.Span.IntersectsWith(candidateLocation.SourceSpan)));
        }

        protected ImmutableArray<ISymbol> GetSymbols(
            INamespaceOrTypeSymbol container,
            int position,
            bool excludeInstance,
            bool useBaseReferenceAccessibility)
        {
            return useBaseReferenceAccessibility
                ? _context.SemanticModel.LookupBaseMembers(position)
                : LookupSymbolsInContainer(container, position, excludeInstance);
        }

        protected ImmutableArray<ISymbol> LookupSymbolsInContainer(
            INamespaceOrTypeSymbol container, int position, bool excludeInstance)
        {
            return excludeInstance
                ? _context.SemanticModel.LookupStaticMembers(position, container)
                : SuppressDefaultTupleElements(
                    container,
                    _context.SemanticModel.LookupSymbols(position, container.WithoutNullability(), includeReducedExtensionMethods: true));
        }

        /// <summary>
        /// If container is a tuple type, any of its tuple element which has a friendly name will cause
        /// the suppression of the corresponding default name (ItemN).
        /// In that case, Rest is also removed.
        /// </summary>
        protected static ImmutableArray<ISymbol> SuppressDefaultTupleElements(
            INamespaceOrTypeSymbol container, ImmutableArray<ISymbol> symbols)
        {
            var namedType = container as INamedTypeSymbol;
            if (namedType?.IsTupleType != true)
            {
                // container is not a tuple
                return symbols;
            }

            //return tuple elements followed by other members that are not fields
            return ImmutableArray<ISymbol>.CastUp(namedType.TupleElements).
                Concat(symbols.WhereAsArray(s => s.Kind != SymbolKind.Field));
        }
    }
}
