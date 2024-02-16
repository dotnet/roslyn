// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Recommendations;

internal abstract partial class AbstractRecommendationService<TSyntaxContext, TAnonymousFunctionSyntax>
{
    protected abstract class AbstractRecommendationServiceRunner
    {
        protected readonly TSyntaxContext _context;
        protected readonly bool _filterOutOfScopeLocals;
        protected readonly CancellationToken _cancellationToken;
        private readonly StringComparer _stringComparerForLanguage;

        public AbstractRecommendationServiceRunner(
            TSyntaxContext context,
            bool filterOutOfScopeLocals,
            CancellationToken cancellationToken)
        {
            _context = context;
            _stringComparerForLanguage = _context.GetLanguageService<ISyntaxFactsService>().StringComparer;
            _filterOutOfScopeLocals = filterOutOfScopeLocals;
            _cancellationToken = cancellationToken;
        }

        public abstract RecommendedSymbols GetRecommendedSymbols();

        protected abstract int GetLambdaParameterCount(TAnonymousFunctionSyntax lambdaSyntax);

        public abstract bool TryGetExplicitTypeOfLambdaParameter(SyntaxNode lambdaSyntax, int ordinalInLambda, [NotNullWhen(returnValue: true)] out ITypeSymbol explicitLambdaParameterType);

        // This code is to help give intellisense in the following case: 
        // query.Include(a => a.SomeProperty).ThenInclude(a => a.
        // where there are more than one overloads of ThenInclude accepting different types of parameters.
        private ImmutableArray<ISymbol> GetMemberSymbolsForParameter(IParameterSymbol parameter, int position, bool useBaseReferenceAccessibility, bool unwrapNullable, bool isForDereference)
        {
            var symbols = TryGetMemberSymbolsForLambdaParameter(parameter, position, unwrapNullable, isForDereference);
            return symbols.IsDefault
                ? GetMemberSymbols(parameter.Type, position, excludeInstance: false, useBaseReferenceAccessibility, unwrapNullable, isForDereference)
                : symbols;
        }

        private ImmutableArray<ISymbol> TryGetMemberSymbolsForLambdaParameter(
            IParameterSymbol parameter,
            int position,
            bool unwrapNullable,
            bool isForDereference)
        {
            // Use normal lookup path for this/base parameters.
            if (parameter.IsThis)
                return default;

            // Starting from a. in the example, looking for a => a.
            if (parameter.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } owningMethod)
                return default;

            // Cannot proceed without DeclaringSyntaxReferences.
            // We expect that there is a single DeclaringSyntaxReferences in the scenario.
            // If anything changes on the compiler side, the approach should be revised.
            if (owningMethod.DeclaringSyntaxReferences.Length != 1)
                return default;

            var syntaxFactsService = _context.GetLanguageService<ISyntaxFactsService>();

            // Check that a => a. belongs to an invocation.
            // Find its' ordinal in the invocation, e.g. ThenInclude(a => a.Something, a=> a.
            if (owningMethod.DeclaringSyntaxReferences.Single().GetSyntax(_cancellationToken) is not TAnonymousFunctionSyntax lambdaSyntax)
                return default;

            if (!(syntaxFactsService.IsArgument(lambdaSyntax.Parent) &&
                  syntaxFactsService.IsInvocationExpression(lambdaSyntax.Parent.Parent.Parent)))
            {
                return default;
            }

            var invocationExpression = lambdaSyntax.Parent.Parent.Parent;
            var arguments = syntaxFactsService.GetArgumentsOfInvocationExpression(invocationExpression);
            var argumentName = syntaxFactsService.GetNameForArgument(lambdaSyntax.Parent);
            var ordinalInInvocation = arguments.IndexOf(lambdaSyntax.Parent);
            var expressionOfInvocationExpression = syntaxFactsService.GetExpressionOfInvocationExpression(invocationExpression);

            var parameterTypeSymbols = ImmutableArray<ITypeSymbol>.Empty;

            if (TryGetExplicitTypeOfLambdaParameter(lambdaSyntax, parameter.Ordinal, out var explicitLambdaParameterType))
            {
                parameterTypeSymbols = [explicitLambdaParameterType];
            }
            else
            {
                // Get all members potentially matching the invocation expression.
                // We filter them out based on ordinality later.
                var candidateSymbols = _context.SemanticModel.GetMemberGroup(expressionOfInvocationExpression, _cancellationToken);

                // parameter.Ordinal is the ordinal within (a,b,c) => b.
                // For candidate symbols of (a,b,c) => b., get types of all possible b.

                // First try to find delegates whose parameter count matches what the user provided.  However, if that
                // finds nothing, fall back to accepting any potential delegates.  We don't want the punish the user if
                // they provide the wrong number while in the middle of working with their code.
                var lambdaParameterCount = this.GetLambdaParameterCount(lambdaSyntax);
                parameterTypeSymbols = GetTypeSymbols(candidateSymbols, argumentName, ordinalInInvocation, parameter.Ordinal, lambdaParameterCount);
                if (parameterTypeSymbols.IsEmpty)
                    parameterTypeSymbols = GetTypeSymbols(candidateSymbols, argumentName, ordinalInInvocation, parameter.Ordinal, lambdaParameterCount: -1);

                // The parameterTypeSymbols may include type parameters, and we want their substituted types if available.
                parameterTypeSymbols = SubstituteTypeParameters(parameterTypeSymbols, invocationExpression);
            }

            // For each type of b., return all suitable members. Also, ensure we consider the actual type of the
            // parameter the compiler inferred as it may have made a completely suitable inference for it.
            // (Only add the actual type if it's not already in the set, otherwise the type and all of its members will be considered twice.)
            if (!parameterTypeSymbols.Contains(parameter.Type, SymbolEqualityComparer.Default))
                parameterTypeSymbols = parameterTypeSymbols.Concat(parameter.Type);

            return parameterTypeSymbols
                .SelectManyAsArray(parameterTypeSymbol =>
                    GetMemberSymbols(parameterTypeSymbol, position, excludeInstance: false, useBaseReferenceAccessibility: false, unwrapNullable, isForDereference));
        }

        private ImmutableArray<ITypeSymbol> SubstituteTypeParameters(ImmutableArray<ITypeSymbol> parameterTypeSymbols, SyntaxNode invocationExpression)
        {
            if (!parameterTypeSymbols.Any(static t => t.IsKind(SymbolKind.TypeParameter)))
            {
                return parameterTypeSymbols;
            }

            var invocationSymbols = _context.SemanticModel.GetSymbolInfo(invocationExpression).GetAllSymbols();
            if (invocationSymbols.Length == 0)
            {
                return parameterTypeSymbols;
            }

            using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var concreteTypes);
            foreach (var invocationSymbol in invocationSymbols)
            {
                var typeParameters = invocationSymbol.GetTypeParameters();
                var typeArguments = invocationSymbol.GetTypeArguments();

                foreach (var parameterTypeSymbol in parameterTypeSymbols)
                {
                    if (parameterTypeSymbol.IsKind<ITypeParameterSymbol>(SymbolKind.TypeParameter, out var typeParameter))
                    {
                        // The typeParameter could be from the containing type, so it may not be
                        // present in this method's list of typeParameters.
                        var index = typeParameters.IndexOf(typeParameter);
                        var concreteType = typeArguments.ElementAtOrDefault(index);

                        // If we couldn't find the concrete type, still consider the typeParameter
                        // as is to provide members of any types it is constrained to (including object)
                        concreteTypes.Add(concreteType ?? typeParameter);
                    }
                    else
                    {
                        concreteTypes.Add(parameterTypeSymbol);
                    }
                }
            }

            return concreteTypes.ToImmutable();
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
        private ImmutableArray<ITypeSymbol> GetTypeSymbols(
            ImmutableArray<ISymbol> candidateSymbols,
            string argumentName,
            int ordinalInInvocation,
            int ordinalInLambda,
            int lambdaParameterCount)
        {
            var expressionSymbol = _context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Expression<>).FullName);

            using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var builder);

            foreach (var candidateSymbol in candidateSymbols)
            {
                if (candidateSymbol is IMethodSymbol method)
                {
                    if (!TryGetMatchingParameterTypeForArgument(method, argumentName, ordinalInInvocation, out var type))
                        continue;

                    // If type is <see cref="Expression{TDelegate}"/>, ignore <see cref="Expression"/> and use TDelegate.
                    // Ignore this check if expressionSymbol is null, e.g. semantic model is broken or incomplete or if the framework does not contain <see cref="Expression"/>.
                    if (expressionSymbol != null &&
                        type is INamedTypeSymbol expressionSymbolNamedTypeCandidate &&
                        expressionSymbolNamedTypeCandidate.OriginalDefinition.Equals(expressionSymbol))
                    {
                        var allTypeArguments = type.GetAllTypeArguments();
                        if (allTypeArguments.Length != 1)
                            continue;

                        type = allTypeArguments[0];
                    }

                    if (type.IsDelegateType())
                    {
                        var methods = type.GetMembers(WellKnownMemberNames.DelegateInvokeName);
                        if (methods.Length != 1)
                            continue;

                        var parameters = methods[0].GetParameters();
                        if (parameters.Length <= ordinalInLambda)
                            continue;

                        if (lambdaParameterCount >= 0 && parameters.Length != lambdaParameterCount)
                            continue;

                        builder.Add(parameters[ordinalInLambda].Type);
                    }
                }
            }

            builder.RemoveDuplicates();
            return builder.ToImmutableAndClear();
        }

        private bool TryGetMatchingParameterTypeForArgument(IMethodSymbol method, string argumentName, int ordinalInInvocation, out ITypeSymbol parameterType)
        {
            if (!string.IsNullOrEmpty(argumentName))
            {
                parameterType = method.Parameters.FirstOrDefault(p => _stringComparerForLanguage.Equals(p.Name, argumentName))?.Type;
                return parameterType != null;
            }

            // We don't know the argument name, so have to find the parameter based on position
            if (method.IsParams() && (ordinalInInvocation >= method.Parameters.Length - 1))
            {
                if (method.Parameters.LastOrDefault()?.Type is IArrayTypeSymbol arrayType)
                {
                    parameterType = arrayType.ElementType;
                    return true;
                }
                else
                {
                    parameterType = null;
                    return false;
                }
            }

            if (ordinalInInvocation < method.Parameters.Length)
            {
                parameterType = method.Parameters[ordinalInInvocation].Type;
                return true;
            }

            parameterType = null;
            return false;
        }

        protected ImmutableArray<ISymbol> GetSymbolsForNamespaceDeclarationNameContext<TNamespaceDeclarationSyntax>()
            where TNamespaceDeclarationSyntax : SyntaxNode
        {
            var declarationSyntax = _context.TargetToken.GetAncestor<TNamespaceDeclarationSyntax>();
            if (declarationSyntax == null)
                return [];

            var semanticModel = _context.SemanticModel;
            var containingNamespaceSymbol = semanticModel.Compilation.GetCompilationNamespace(
                semanticModel.GetEnclosingNamespace(declarationSyntax.SpanStart, _cancellationToken));

            var symbols = semanticModel.LookupNamespacesAndTypes(declarationSyntax.SpanStart, containingNamespaceSymbol)
                                       .WhereAsArray(recommendationSymbol => IsNonIntersectingNamespace(recommendationSymbol, declarationSyntax));

            return symbols;
        }

        protected ImmutableArray<ISymbol> GetSymbolsForEnumBaseList(INamespaceOrTypeSymbol container)
        {
            var semanticModel = _context.SemanticModel;
            var systemNamespace = container is not (null or INamespaceSymbol { IsGlobalNamespace: true })
                ? null
                 : semanticModel.LookupNamespacesAndTypes(_context.Position, semanticModel.Compilation.GlobalNamespace, nameof(System))
                     .OfType<INamespaceSymbol>().FirstOrDefault();

            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);

            if (systemNamespace is not null)
            {
                builder.Add(systemNamespace);

                var aliases = semanticModel.LookupSymbols(_context.Position, container).OfType<IAliasSymbol>().Where(a => systemNamespace.Equals(a.Target));
                builder.AddRange(aliases);
            }

            AddSpecialTypeSymbolAndItsAliases(nameof(Byte), SpecialType.System_Byte);
            AddSpecialTypeSymbolAndItsAliases(nameof(SByte), SpecialType.System_SByte);
            AddSpecialTypeSymbolAndItsAliases(nameof(Int16), SpecialType.System_Int16);
            AddSpecialTypeSymbolAndItsAliases(nameof(UInt16), SpecialType.System_UInt16);
            AddSpecialTypeSymbolAndItsAliases(nameof(Int32), SpecialType.System_Int32);
            AddSpecialTypeSymbolAndItsAliases(nameof(UInt32), SpecialType.System_UInt32);
            AddSpecialTypeSymbolAndItsAliases(nameof(Int64), SpecialType.System_Int64);
            AddSpecialTypeSymbolAndItsAliases(nameof(UInt64), SpecialType.System_UInt64);

            return builder.ToImmutableAndClear();

            void AddSpecialTypeSymbolAndItsAliases(string name, SpecialType specialType)
            {
                var specialTypeSymbol = _context.SemanticModel
                    .LookupNamespacesAndTypes(_context.Position, container, name)
                    .FirstOrDefault(s => s is INamedTypeSymbol namedType && namedType.SpecialType == specialType);

                builder.AddIfNotNull(specialTypeSymbol);

                specialTypeSymbol ??= _context.SemanticModel.Compilation.GetSpecialType(specialType);

                var aliases = _context.SemanticModel.LookupSymbols(_context.Position, container).OfType<IAliasSymbol>().Where(a => specialTypeSymbol.Equals(a.Target));
                builder.AddRange(aliases);
            }
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
                       static (candidateLocation, declarationSyntax) => !(declarationSyntax.SyntaxTree == candidateLocation.SourceTree &&
                                              declarationSyntax.Span.IntersectsWith(candidateLocation.SourceSpan)), declarationSyntax);
        }

        protected ImmutableArray<ISymbol> GetMemberSymbols(
            ISymbol container,
            int position,
            bool excludeInstance,
            bool useBaseReferenceAccessibility,
            bool unwrapNullable,
            bool isForDereference)
        {
            // For a normal parameter, we have a specialized codepath we use to ensure we properly get lambda parameter
            // information that the compiler may fail to give.
            if (container is IParameterSymbol parameter)
                return GetMemberSymbolsForParameter(parameter, position, useBaseReferenceAccessibility, unwrapNullable, isForDereference);

            if (isForDereference && container is IPointerTypeSymbol pointerType)
            {
                container = pointerType.PointedAtType;
            }

            if (container is not INamespaceOrTypeSymbol namespaceOrType)
                return [];

            if (unwrapNullable && namespaceOrType is ITypeSymbol typeSymbol)
            {
                namespaceOrType = typeSymbol.RemoveNullableIfPresent();
            }

            return useBaseReferenceAccessibility
                ? _context.SemanticModel.LookupBaseMembers(position)
                : LookupSymbolsInContainer(namespaceOrType, position, excludeInstance);
        }

        protected ImmutableArray<ISymbol> LookupSymbolsInContainer(
            INamespaceOrTypeSymbol container, int position, bool excludeInstance)
        {
            if (excludeInstance)
                return _context.SemanticModel.LookupStaticMembers(position, container);

            var containerMembers = SuppressDefaultTupleElements(
                container,
                _context.SemanticModel.LookupSymbols(position, container, includeReducedExtensionMethods: true));

            if (container is not ITypeSymbol containerType)
                return containerMembers;

            // Compiler will return reduced extension methods in the case it can't determine if constraints match.
            // Attempt to filter out cases we have strong confidence will never succeed.
            using var _ = ArrayBuilder<ISymbol>.GetInstance(containerMembers.Length, out var result);

            foreach (var member in containerMembers)
            {
                if (member.IsReducedExtension())
                {
                    // Get the original extension method and see if it extends a type parameter that itself has any
                    // base-type or base-interface constraints. If so, confirm that the type we're on derives from or
                    // implements that constraint types.  Note that we do this looking at the uninstantiated forms as
                    // there's no way to tell if the instantiations match as the signature may not have enough
                    // information provided to answer that question accurately.
                    var originalMember = member.GetOriginalUnreducedDefinition();
                    if (originalMember is IMethodSymbol { Parameters: [{ Type: ITypeParameterSymbol parameterType }, ..] })
                    {
                        if (!MatchesConstraints(containerType.OriginalDefinition, parameterType.ConstraintTypes))
                            continue;
                    }
                }

                result.Add(member);
            }

            return result.ToImmutable();

            static bool MatchesConstraints(ITypeSymbol originalContainerType, ImmutableArray<ITypeSymbol> constraintTypes)
            {
                // If there are no constraint types, then this type parameter was unconstrained, so could match anything.
                if (constraintTypes.IsEmpty)
                    return true;

                // Now check that the type we're calling on matched at least one of the constraints that were specified.
                foreach (var constraintType in constraintTypes)
                {
                    if (MatchesConstraint(originalContainerType, constraintType.OriginalDefinition))
                        return true;
                }

                return false;
            }

            static bool MatchesConstraint(ITypeSymbol originalContainerType, ITypeSymbol originalConstraintType)
            {
                // If the type we're dotting off of *is* the constraint type, then this is def a match and we can proceed.
                if (SymbolEqualityComparer.Default.Equals(originalContainerType, originalConstraintType))
                    return true;

                if (originalConstraintType.TypeKind == TypeKind.TypeParameter)
                {
                    // If it's a type parameter constrained on another type parameter, then just assume for now that
                    // it's a match.  We could attempt to walk through these in the future, but for now this is complex
                    // enough that we'll just allow it.
                    return true;
                }
                else if (originalConstraintType.TypeKind == TypeKind.Interface)
                {
                    // If the constraint is an interface then see if that interface appears in the interface inheritance
                    // hierarchy of the type we're dotting off of.
                    foreach (var interfaceType in originalContainerType.AllInterfaces)
                    {
                        if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, originalConstraintType))
                            return true;
                    }
                }
                else if (originalConstraintType.TypeKind == TypeKind.Class)
                {
                    // If the constraint is an interface then see if that interface appears in the base type inheritance
                    // hierarchy of the type we're dotting off of.
                    for (var current = originalContainerType.BaseType; current != null; current = current.BaseType)
                    {
                        if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, originalConstraintType))
                            return true;
                    }
                }
                else
                {
                    // If we somehow have a constraint that isn't a type parameter, or class, or interface, then we
                    // really don't know what's going on.  Just presume that this constraint would match and show the
                    // completion item.  We can revisit this choice if this turns out to be an issue.
                    return true;
                }

                // For anything else, we don't consider this a match.  This can be adjusted in the future if need be.
                return false;
            }
        }

        /// <summary>
        /// If container is a tuple type, any of its tuple element which has a friendly name will cause the suppression
        /// of the corresponding default name (ItemN). In that case, Rest is also removed.
        /// </summary>
        protected static ImmutableArray<ISymbol> SuppressDefaultTupleElements(INamespaceOrTypeSymbol container, ImmutableArray<ISymbol> symbols)
            => container is not INamedTypeSymbol { IsTupleType: true } namedType
                ? symbols
                : symbols.Where(s => s is not IFieldSymbol).Concat(namedType.TupleElements).ToImmutableArray();
    }
}
