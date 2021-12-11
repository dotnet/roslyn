// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

partial class DelegateCacheRewriter
{
    /// <summary>
    /// Checks if a type or a method is constructed with some type that involves the specified generic type parameters.
    /// This checker is different with <see cref="TypeSymbolExtensions.ContainsTypeParameters"/> because here we check method symbols, anonmous types, as well as custom modifiers.
    /// </summary>
    sealed class TypeParameterUsageChecker : CSharpSymbolVisitor<HashSet<TypeParameterSymbol>, bool>
    {
        public static TypeParameterUsageChecker Instance { get; } = new();

        public override bool VisitTypeParameter(TypeParameterSymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => typeParameters.Contains(symbol);

        public override bool VisitArrayType(ArrayTypeSymbol symbol, HashSet<TypeParameterSymbol> typeParameters)
        {
            return VisitTypeWithAnnotations(symbol.ElementTypeWithAnnotations, typeParameters);
        }

        public override bool VisitNamedType(NamedTypeSymbol symbol, HashSet<TypeParameterSymbol> typeParameters)
        {
            if (symbol.IsAnonymousType)
            {
                switch (symbol.TypeKind)
                {
                    case TypeKind.Class:
                        {
                            var anonymousClass = (AnonymousTypeManager.AnonymousTypePublicSymbol)symbol;

                            foreach (var property in anonymousClass.Properties)
                            {
                                if (VisitTypeWithAnnotations(property.TypeWithAnnotations, typeParameters))
                                {
                                    return true;
                                }
                            }
                        }
                        break;

                    case TypeKind.Delegate:
                        {
                            var anonymousDelegate = (AnonymousTypeManager.AnonymousDelegatePublicSymbol)symbol;

                            foreach (var typeArgumentWithAnnotations in anonymousDelegate.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
                            {
                                if (VisitTypeWithAnnotations(typeArgumentWithAnnotations, typeParameters))
                                {
                                    return true;
                                }
                            }
                        }
                        break;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }

                return false;
            }

            if (symbol.IsTupleType)
            {
                foreach (var elementTypeWithAnnotations in symbol.TupleElementTypesWithAnnotations)
                {
                    if (VisitTypeWithAnnotations(elementTypeWithAnnotations, typeParameters))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var typeArgumentWithAnnotations in symbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
            {
                if (VisitTypeWithAnnotations(typeArgumentWithAnnotations, typeParameters))
                {
                    return true;
                }
            }

            return Visit(symbol.ContainingType, typeParameters);
        }

        public override bool VisitMethod(MethodSymbol symbol, HashSet<TypeParameterSymbol> typeParameters)
        {
            foreach (var typeArgumentWithAnnotations in symbol.TypeArgumentsWithAnnotations)
            {
                if (VisitTypeWithAnnotations(typeArgumentWithAnnotations, typeParameters))
                {
                    return true;
                }
            }

            return Visit(symbol.ContainingType, typeParameters);
        }

        public override bool VisitPointerType(PointerTypeSymbol symbol, HashSet<TypeParameterSymbol> typeParameters)
        {
            // Func<int*[]> is a good example why here is reachable.
            return VisitTypeWithAnnotations(symbol.PointedAtTypeWithAnnotations, typeParameters);
        }

        private bool VisitTypeWithAnnotations(in TypeWithAnnotations typeWithAnnotations, HashSet<TypeParameterSymbol> typeParameters)
        {
            if (Visit(typeWithAnnotations.Type, typeParameters))
            {
                return true;
            }

            foreach (var customModifier in typeWithAnnotations.CustomModifiers)
            {
                if (Visit((Symbol)customModifier.Modifier, typeParameters))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool VisitAssembly(AssemblySymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;

        public override bool VisitModule(ModuleSymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;

        public override bool VisitEvent(EventSymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;

        public override bool VisitProperty(PropertySymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;

        public override bool VisitField(FieldSymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;

        public override bool VisitParameter(ParameterSymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;

        public override bool VisitLocal(LocalSymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;

        public override bool VisitRangeVariable(RangeVariableSymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;

        public override bool VisitLabel(LabelSymbol symbol, HashSet<TypeParameterSymbol> typeParameters) => throw ExceptionUtilities.Unreachable;
    }
}
