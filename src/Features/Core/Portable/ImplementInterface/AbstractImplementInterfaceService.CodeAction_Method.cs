// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        private sealed class InterfaceReplacerWithContainingTypeVisistor : SymbolVisitor<ISymbol>
        {
            private readonly INamedTypeSymbol _interfaceSymbol;
            private readonly INamedTypeSymbol _containingTypeSymbol;

            public InterfaceReplacerWithContainingTypeVisistor(INamedTypeSymbol interfaceSymbol, INamedTypeSymbol containingTypeSymbol)
            {
                Debug.Assert(interfaceSymbol.TypeKind == TypeKind.Interface);
                Debug.Assert(containingTypeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct);
                _interfaceSymbol = interfaceSymbol;
                _containingTypeSymbol = containingTypeSymbol;
            }

            public override ISymbol VisitMethod(IMethodSymbol symbol)
            {
                Debug.Assert(symbol.MethodKind == MethodKind.UserDefinedOperator);
                if (symbol.Parameters.Length == 0)
                {
                    return symbol;
                }

                var updatedParameters = ImmutableArray.CreateBuilder<IParameterSymbol>(initialCapacity: symbol.Parameters.Length);
                foreach (var parameter in symbol.Parameters)
                {
                    updatedParameters.Add((IParameterSymbol)VisitParameter(parameter));
                }

                var returnType = symbol.ReturnType;
                if (returnType is INamedTypeSymbol namedTypeSymbol)
                {
                    returnType = (INamedTypeSymbol)VisitNamedType(namedTypeSymbol);
                }

                return CodeGenerationSymbolFactory.CreateMethodSymbol(symbol, parameters: updatedParameters.ToImmutable(), returnType: returnType);
            }

            public override ISymbol VisitParameter(IParameterSymbol symbol)
            {
                if (symbol.Type is INamedTypeSymbol namedTypeSymbol)
                {
                    return CodeGenerationSymbolFactory.CreateParameterSymbol(symbol, type: (INamedTypeSymbol)VisitNamedType(namedTypeSymbol));
                }

                return symbol;
            }

            public override ISymbol VisitNamedType(INamedTypeSymbol symbol)
            {
                if (symbol.Equals(_interfaceSymbol, SymbolEqualityComparer.Default))
                {
                    return _containingTypeSymbol;
                }

                return symbol;
            }
        }

        internal partial class ImplementInterfaceCodeAction
        {
            private ISymbol GenerateMethod(
                Compilation compilation,
                IMethodSymbol method,
                Accessibility accessibility,
                DeclarationModifiers modifiers,
                bool generateAbstractly,
                bool useExplicitInterfaceSymbol,
                string memberName)
            {
                var syntaxFacts = Document.GetRequiredLanguageService<ISyntaxFactsService>();

                var updatedMethod = method.EnsureNonConflictingNames(State.ClassOrStructType, syntaxFacts);

                updatedMethod = updatedMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(
                    State.ClassOrStructType,
                    AttributesToRemove(compilation));

                // Operators parameter should match containing type. For example, implementing:
                // interface I { static abstract int operator -(I x); }
                // in a class called C should result in:
                // class C : I { public static int operator -(C x) { } }
                updatedMethod = (IMethodSymbol)updatedMethod.Accept(new InterfaceReplacerWithContainingTypeVisistor(updatedMethod.ContainingType, State.ClassOrStructType));

                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    updatedMethod,
                    accessibility: accessibility,
                    modifiers: modifiers,
                    explicitInterfaceImplementations: useExplicitInterfaceSymbol ? ImmutableArray.Create(updatedMethod) : default,
                    name: memberName,
                    statements: generateAbstractly
                        ? default
                        : ImmutableArray.Create(CreateStatement(compilation, updatedMethod)));
            }

            private SyntaxNode CreateStatement(Compilation compilation, IMethodSymbol method)
            {
                var factory = Document.GetLanguageService<SyntaxGenerator>();
                return ThroughMember == null
                    ? factory.CreateThrowNotImplementedStatement(compilation)
                    : factory.GenerateDelegateThroughMemberStatement(method, ThroughMember);
            }
        }
    }
}
