// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            var element = symbol.ElementTypeWithAnnotations;

            if (Visit(element.Type, typeParameters))
            {
                return true;
            }

            if (VisitCustomModifiers(element.CustomModifiers, typeParameters))
            {
                return true;
            }

            return false;
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
                                if (Visit(property.Type, typeParameters))
                                {
                                    return true;
                                }
                            }
                        }
                        break;

                    case TypeKind.Delegate:
                        {
                            var anonymousDelegate = (AnonymousTypeManager.AnonymousDelegatePublicSymbol)symbol;

                            foreach (var typeArg in anonymousDelegate.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
                            {
                                if (Visit(typeArg.Type, typeParameters))
                                {
                                    return true;
                                }

                                if (VisitCustomModifiers(typeArg.CustomModifiers, typeParameters))
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
                foreach (var property in symbol.TupleElementTypesWithAnnotations)
                {
                    if (Visit(property.Type, typeParameters))
                    {
                        return true;
                    }

                    if (VisitCustomModifiers(property.CustomModifiers, typeParameters))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var typeArg in symbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
            {
                if (Visit(typeArg.Type, typeParameters))
                {
                    return true;
                }

                if (VisitCustomModifiers(typeArg.CustomModifiers, typeParameters))
                {
                    return true;
                }
            }

            return Visit(symbol.ContainingType, typeParameters);
        }

        public override bool VisitMethod(MethodSymbol symbol, HashSet<TypeParameterSymbol> typeParameters)
        {
            foreach (var typeArg in symbol.TypeArgumentsWithAnnotations)
            {
                if (Visit(typeArg.Type, typeParameters))
                {
                    return true;
                }

                if (VisitCustomModifiers(typeArg.CustomModifiers, typeParameters))
                {
                    return true;
                }
            }

            return Visit(symbol.ContainingType, typeParameters);
        }

        public override bool VisitPointerType(PointerTypeSymbol symbol, HashSet<TypeParameterSymbol> typeParameters)
        {
            // Func<int*[]> is a good example why here is reachable.

            var pointedAt = symbol.PointedAtTypeWithAnnotations;

            if (Visit(pointedAt.Type, typeParameters))
            {
                return true;
            }

            if (VisitCustomModifiers(pointedAt.CustomModifiers, typeParameters))
            {
                return true;
            }

            return false;
        }

        private bool VisitCustomModifiers(ImmutableArray<CustomModifier> customModifiers, HashSet<TypeParameterSymbol> typeParameters)
        {
            foreach (var customModifier in customModifiers)
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
