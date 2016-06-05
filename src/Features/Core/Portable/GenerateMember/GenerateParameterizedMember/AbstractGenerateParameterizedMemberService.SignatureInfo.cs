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
            private IList<ITypeParameterSymbol> _typeParameters;
            private IDictionary<ITypeSymbol, ITypeParameterSymbol> _typeArgumentToTypeParameterMap;

            public SignatureInfo(
                SemanticDocument document,
                State state)
            {
                this.Document = document;
                this.State = state;
            }

            public IList<ITypeParameterSymbol> DetermineTypeParameters(CancellationToken cancellationToken)
            {
                return _typeParameters ?? (_typeParameters = DetermineTypeParametersWorker(cancellationToken));
            }

            protected abstract IList<ITypeParameterSymbol> DetermineTypeParametersWorker(CancellationToken cancellationToken);

            public ITypeSymbol DetermineReturnType(CancellationToken cancellationToken)
            {
                return FixType(DetermineReturnTypeWorker(cancellationToken), cancellationToken);
            }

            protected abstract IList<ITypeSymbol> DetermineTypeArguments(CancellationToken cancellationToken);
            protected abstract ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken);
            protected abstract IList<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken);
            protected abstract IList<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken);
            protected abstract IList<bool> DetermineParameterOptionality(CancellationToken cancellationToken);
            protected abstract IList<ParameterName> DetermineParameterNames(CancellationToken cancellationToken);

            internal IPropertySymbol GenerateProperty(
                SyntaxGenerator factory,
                bool isAbstract, bool includeSetter,
                CancellationToken cancellationToken)
            {
                var accessibility = DetermineAccessibility(isAbstract);
                var getMethod = CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    attributes: null,
                    accessibility: accessibility,
                    statements: GenerateStatements(factory, isAbstract, cancellationToken));

                var setMethod = includeSetter ? getMethod : null;

                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    attributes: null,
                    accessibility: accessibility,
                    modifiers: new DeclarationModifiers(isStatic: State.IsStatic, isAbstract: isAbstract),
                    type: DetermineReturnType(cancellationToken),
                    explicitInterfaceSymbol: null,
                    name: this.State.IdentifierToken.ValueText,
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
                var isUnsafe = (parameters
                    .Any(p => p.Type.IsUnsafe()) || returnType.IsUnsafe()) &&
                    !State.IsContainedInUnsafeType;
                var method = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: null,
                    accessibility: DetermineAccessibility(isAbstract),
                    modifiers: new DeclarationModifiers(isStatic: State.IsStatic, isAbstract: isAbstract, isUnsafe: isUnsafe),
                    returnType: returnType,
                    explicitInterfaceSymbol: null,
                    name: this.State.IdentifierToken.ValueText,
                    typeParameters: DetermineTypeParameters(cancellationToken),
                    parameters: parameters,
                    statements: GenerateStatements(factory, isAbstract, cancellationToken),
                    handlesExpressions: null,
                    returnTypeAttributes: null,
                    methodKind: State.MethodKind);

                // Ensure no conflicts between type parameter names and parameter names.
                var languageServiceProvider = this.Document.Project.Solution.Workspace.Services.GetLanguageServices(this.State.TypeToGenerateIn.Language);
                var syntaxFacts = languageServiceProvider.GetService<ISyntaxFactsService>();

                var equalityComparer = syntaxFacts.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
                var reservedParameterNames = this.DetermineParameterNames(cancellationToken)
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
                var availableMethodTypeParameters = this.DetermineTypeParameters(cancellationToken);
                var availableTypeParameters = this.State.TypeToGenerateIn.GetAllTypeParameters();

                var compilation = this.Document.SemanticModel.Compilation;
                var allTypeParameters = availableMethodTypeParameters.Concat(availableTypeParameters);

                var typeArgumentToTypeParameterMap = this.GetTypeArgumentToTypeParameterMap(cancellationToken);

                return typeSymbol.RemoveAnonymousTypes(compilation)
                                 .ReplaceTypeParametersBasedOnTypeConstraints(compilation, allTypeParameters, this.Document.Document.Project.Solution, cancellationToken)
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
                var typeArguments = this.DetermineTypeArguments(cancellationToken);
                var typeParameters = this.DetermineTypeParameters(cancellationToken);

                var result = new Dictionary<ITypeSymbol, ITypeParameterSymbol>();

                for(var i = 0; i < typeArguments.Count; i++)
                {
                    if (typeArguments[i] != null)
                    {
                        result[typeArguments[i]] = typeParameters[i];
                    }
                }

                return result;
            }

            private IList<SyntaxNode> GenerateStatements(
                SyntaxGenerator factory,
                bool isAbstract,
                CancellationToken cancellationToken)
            {
                var throwStatement = CodeGenerationHelpers.GenerateThrowStatement(factory, this.Document, "System.NotImplementedException", cancellationToken);

                return isAbstract || State.TypeToGenerateIn.TypeKind == TypeKind.Interface || throwStatement == null
                    ? null
                    : new[] { throwStatement };
            }

            private IList<IParameterSymbol> DetermineParameters(CancellationToken cancellationToken)
            {
                var modifiers = DetermineParameterModifiers(cancellationToken);
                var types = DetermineParameterTypes(cancellationToken).Select(t => FixType(t, cancellationToken)).ToList();
                var optionality = DetermineParameterOptionality(cancellationToken);
                var names = DetermineParameterNames(cancellationToken);

                var result = new List<IParameterSymbol>();
                for (var i = 0; i < modifiers.Count; i++)
                {
                    result.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: null,
                        refKind: modifiers[i],
                        isParams: false,
                        isOptional: optionality[i],
                        type: types[i],
                        name: names[i].BestNameForParameter));
                }

                return result;
            }

            private Accessibility DetermineAccessibility(bool isAbstract)
            {
                var containingType = this.State.ContainingType;

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
                        // class B : A { void Foo() { A a; a.Foo(); }
                        //
                        // In this case we can *not* mark the method as protected.  'B' can only
                        // access protected members of 'A' through an instance of 'B' (or a subclass
                        // of B).  It can not access protected members through an instance of the
                        // superclass.  In this case we need to make the method public or internal.
                        //
                        // However, this does not apply if the method will be static.  i.e.
                        // 
                        // class B : A { void Foo() { A.Foo(); }
                        //
                        // B can access the protected statics of A, and so we generate 'Foo' as
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
