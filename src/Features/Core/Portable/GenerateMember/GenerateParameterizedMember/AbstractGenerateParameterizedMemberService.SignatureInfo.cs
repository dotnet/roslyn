// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal abstract partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        internal abstract class SignatureInfo
        {
            protected readonly SemanticDocument Document;
            protected readonly State State;
            private ImmutableArray<ITypeParameterSymbol> _typeParameters;
            private IDictionary<ITypeSymbol, ITypeParameterSymbol> _typeArgumentToTypeParameterMap;

            public SignatureInfo(
                SemanticDocument document,
                State state)
            {
                Document = document;
                State = state;
            }

            public ImmutableArray<ITypeParameterSymbol> DetermineTypeParameters(CancellationToken cancellationToken)
            {
                return _typeParameters.IsDefault
                    ? (_typeParameters = DetermineTypeParametersWorker(cancellationToken))
                    : _typeParameters;
            }

            protected abstract ImmutableArray<ITypeParameterSymbol> DetermineTypeParametersWorker(CancellationToken cancellationToken);
            protected abstract RefKind DetermineRefKind(CancellationToken cancellationToken);

            public ITypeSymbol DetermineReturnType(CancellationToken cancellationToken)
            {
                var type = DetermineReturnTypeWorker(cancellationToken);
                if (State.IsInConditionalAccessExpression)
                {
                    type = type.RemoveNullableIfPresent();
                }

                return FixType(type, cancellationToken);
            }

            protected abstract ImmutableArray<ITypeSymbol> DetermineTypeArguments(CancellationToken cancellationToken);
            protected abstract ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken);
            protected abstract ImmutableArray<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken);
            protected abstract ImmutableArray<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken);
            protected abstract ImmutableArray<bool> DetermineParameterOptionality(CancellationToken cancellationToken);
            protected abstract ImmutableArray<ParameterName> DetermineParameterNames(CancellationToken cancellationToken);

            internal IPropertySymbol GenerateProperty(
                SyntaxGenerator factory,
                bool isAbstract, bool includeSetter,
                CancellationToken cancellationToken)
            {
                var accessibility = DetermineAccessibility(isAbstract);
                var getMethod = CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    attributes: default,
                    accessibility: accessibility,
                    statements: GenerateStatements(factory, isAbstract, cancellationToken));

                var setMethod = includeSetter ? getMethod : null;

                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    attributes: default,
                    accessibility: accessibility,
                    modifiers: new DeclarationModifiers(isStatic: State.IsStatic, isAbstract: isAbstract),
                    type: DetermineReturnType(cancellationToken),
                    refKind: DetermineRefKind(cancellationToken),
                    explicitInterfaceImplementations: default,
                    name: State.IdentifierToken.ValueText,
                    parameters: DetermineParameters(cancellationToken),
                    getMethod: getMethod,
                    setMethod: setMethod);
            }

            public IMethodSymbol GenerateMethod(
                SyntaxGenerator factory,
                bool isAbstract,
                CancellationToken cancellationToken)
            {
                var parameters = DetermineParameters(cancellationToken);
                var returnType = DetermineReturnType(cancellationToken);
                var isUnsafe = false;
                if (!State.IsContainedInUnsafeType)
                {
                    isUnsafe = returnType.IsUnsafe() || parameters.Any(p => p.Type.IsUnsafe());
                }

                var method = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: default,
                    accessibility: DetermineAccessibility(isAbstract),
                    modifiers: new DeclarationModifiers(isStatic: State.IsStatic, isAbstract: isAbstract, isUnsafe: isUnsafe),
                    returnType: returnType,
                    refKind: DetermineRefKind(cancellationToken),
                    explicitInterfaceImplementations: default,
                    name: State.IdentifierToken.ValueText,
                    typeParameters: DetermineTypeParameters(cancellationToken),
                    parameters: parameters,
                    statements: GenerateStatements(factory, isAbstract, cancellationToken),
                    handlesExpressions: default,
                    returnTypeAttributes: default,
                    methodKind: State.MethodKind);

                // Ensure no conflicts between type parameter names and parameter names.
                var languageServiceProvider = Document.Project.Solution.Workspace.Services.GetLanguageServices(State.TypeToGenerateIn.Language);
                var syntaxFacts = languageServiceProvider.GetService<ISyntaxFactsService>();

                var equalityComparer = syntaxFacts.StringComparer;
                var reservedParameterNames = DetermineParameterNames(cancellationToken)
                                                 .Select(p => p.BestNameForParameter)
                                                 .ToSet(equalityComparer);
                var newTypeParameterNames = NameGenerator.EnsureUniqueness(
                    method.TypeParameters.Select(t => t.Name).ToList(), n => !reservedParameterNames.Contains(n));

                return method.RenameTypeParameters(newTypeParameterNames);
            }

            private ITypeSymbol FixType(
                ITypeSymbol typeSymbol,
                CancellationToken cancellationToken)
            {
                // A type can't refer to a type parameter that isn't available in the type we're
                // eventually generating into.
                var availableMethodTypeParameters = DetermineTypeParameters(cancellationToken);
                var availableTypeParameters = State.TypeToGenerateIn.GetAllTypeParameters();

                var compilation = Document.SemanticModel.Compilation;
                var allTypeParameters = availableMethodTypeParameters.Concat(availableTypeParameters);

                var typeArgumentToTypeParameterMap = GetTypeArgumentToTypeParameterMap(cancellationToken);

                return typeSymbol.RemoveAnonymousTypes(compilation)
                                 .ReplaceTypeParametersBasedOnTypeConstraints(compilation, allTypeParameters, Document.Document.Project.Solution, cancellationToken)
                                 .RemoveUnavailableTypeParameters(compilation, allTypeParameters)
                                 .RemoveUnnamedErrorTypes(compilation)
                                 .SubstituteTypes(typeArgumentToTypeParameterMap, new TypeGenerator());
            }

            private IDictionary<ITypeSymbol, ITypeParameterSymbol> GetTypeArgumentToTypeParameterMap(
                CancellationToken cancellationToken)
            {
                return _typeArgumentToTypeParameterMap ?? (_typeArgumentToTypeParameterMap = CreateTypeArgumentToTypeParameterMap(cancellationToken));
            }

            private IDictionary<ITypeSymbol, ITypeParameterSymbol> CreateTypeArgumentToTypeParameterMap(
                CancellationToken cancellationToken)
            {
                var typeArguments = DetermineTypeArguments(cancellationToken);
                var typeParameters = DetermineTypeParameters(cancellationToken);

                // We use a nullability-ignoring comparer because top-level and nested nullability won't matter. If we are looking to replace
                // IEnumerable<string> with T, we want to replace IEnumerable<string?> whenever it appears in an argument or return type, partly because
                // there's no way to represent something like T-with-only-the-inner-thing-nullable. We could leave the entire argument as is, but we're suspecting
                // this is closer to the user's desire, even if it might require some tweaking after the fact.
                var result = new Dictionary<ITypeSymbol, ITypeParameterSymbol>(AllNullabilityIgnoringSymbolComparer.Instance);

                for (var i = 0; i < typeArguments.Length; i++)
                {
                    if (typeArguments[i] != null)
                    {
                        result[typeArguments[i]] = typeParameters[i];
                    }
                }

                return result;
            }

            private ImmutableArray<SyntaxNode> GenerateStatements(
                SyntaxGenerator factory,
                bool isAbstract,
                CancellationToken cancellationToken)
            {
                var throwStatement = CodeGenerationHelpers.GenerateThrowStatement(factory, Document, "System.NotImplementedException", cancellationToken);

                return isAbstract || State.TypeToGenerateIn.TypeKind == TypeKind.Interface || throwStatement == null
                    ? default
                    : ImmutableArray.Create(throwStatement);
            }

            private ImmutableArray<IParameterSymbol> DetermineParameters(CancellationToken cancellationToken)
            {
                var modifiers = DetermineParameterModifiers(cancellationToken);
                var types = DetermineParameterTypes(cancellationToken).Select(t => FixType(t, cancellationToken)).ToList();
                var optionality = DetermineParameterOptionality(cancellationToken);
                var names = DetermineParameterNames(cancellationToken);

                var result = ArrayBuilder<IParameterSymbol>.GetInstance();
                for (var i = 0; i < modifiers.Length; i++)
                {
                    result.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default,
                        refKind: modifiers[i],
                        isParams: false,
                        isOptional: optionality[i],
                        type: types[i],
                        name: names[i].BestNameForParameter));
                }

                return result.ToImmutableAndFree();
            }

            private Accessibility DetermineAccessibility(bool isAbstract)
            {
                var containingType = State.ContainingType;

                // If we're generating into an interface, then we don't use any modifiers.
                if (State.TypeToGenerateIn.TypeKind != TypeKind.Interface)
                {
                    // Otherwise, figure out what accessibility modifier to use and optionally
                    // mark it as static.
                    if (containingType.IsContainedWithin(State.TypeToGenerateIn) && !isAbstract)
                    {
                        return Accessibility.Private;
                    }
                    else if (DerivesFrom(containingType) && State.IsStatic)
                    {
                        // NOTE(cyrusn): We only generate protected in the case of statics.  Consider
                        // the case where we're generating into one of our base types.  i.e.:
                        //
                        // class B : A { void Goo() { A a; a.Goo(); }
                        //
                        // In this case we can *not* mark the method as protected.  'B' can only
                        // access protected members of 'A' through an instance of 'B' (or a subclass
                        // of B).  It can not access protected members through an instance of the
                        // superclass.  In this case we need to make the method public or internal.
                        //
                        // However, this does not apply if the method will be static.  i.e.
                        // 
                        // class B : A { void Goo() { A.Goo(); }
                        //
                        // B can access the protected statics of A, and so we generate 'Goo' as
                        // protected.

                        // TODO: Code coverage
                        return Accessibility.Protected;
                    }
                    else if (containingType.ContainingAssembly.IsSameAssemblyOrHasFriendAccessTo(State.TypeToGenerateIn.ContainingAssembly))
                    {
                        return Accessibility.Internal;
                    }
                    else
                    {
                        // TODO: Code coverage
                        return Accessibility.Public;
                    }
                }

                return Accessibility.NotApplicable;
            }

            private bool DerivesFrom(INamedTypeSymbol containingType)
            {
                return containingType.GetBaseTypes().Select(t => t.OriginalDefinition)
                                                    .OfType<INamedTypeSymbol>()
                                                    .Contains(State.TypeToGenerateIn);
            }
        }
    }
}
