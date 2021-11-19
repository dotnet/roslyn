// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    using TypeParams = ImmutableArray<TypeParameterSymbol>;

    /// <summary>
    /// Checks whether a type or a method is constructed with some type that involves the specified generic type parameters.
    /// </summary>
    internal class TypeParameterUsageChecker : CSharpSymbolVisitor<TypeParams, bool>
    {
        public static TypeParameterUsageChecker Instance { get; } = new TypeParameterUsageChecker();

        public override bool VisitTypeParameter(TypeParameterSymbol symbol, TypeParams argument) => argument.Contains(symbol);

        public override bool VisitArrayType(ArrayTypeSymbol symbol, TypeParams argument)
        {
            if (Visit(symbol.ElementType, argument))
            {
                return true;
            }

            if (VisitCustomModifiers(symbol.CustomModifiers, argument))
            {
                return true;
            }

            return false;
        }

        public override bool VisitNamedType(NamedTypeSymbol symbol, TypeParams argument)
        {
            if (symbol.IsAnonymousType)
            {
                foreach (var anonPropType in AnonymousTypeManager.GetAnonymousTypePropertyTypes(symbol))
                {
                    if (Visit(anonPropType, argument))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var typeArg in symbol.TypeArguments)
            {
                if (Visit(typeArg, argument))
                {
                    return true;
                }
            }

            if (symbol.HasTypeArgumentsCustomModifiers)
            {
                var count = symbol.TypeArgumentsNoUseSiteDiagnostics.Length;
                for (int i = 0; i < count; i++)
                {
                    if (VisitCustomModifiers(symbol.GetTypeArgumentCustomModifiers(i), argument))
                    {
                        return true;
                    }
                }
            }

            return Visit(symbol.ContainingType, argument);
        }

        public override bool VisitMethod(MethodSymbol symbol, TypeParams argument)
        {
            foreach (var typeArg in symbol.TypeArguments)
            {
                if (Visit(typeArg, argument))
                {
                    return true;
                }
            }

            return Visit(symbol.ContainingType, argument);
        }

        public override bool VisitPointerType(PointerTypeSymbol symbol, TypeParams argument)
        {
            // Func<int*[]> is a good example why here is reachable.

            // Although PointedAtType cannot be generic by code, we don't know what can come from metadata.
            if (Visit(symbol.PointedAtType, argument))
            {
                return true;
            }

            if (VisitCustomModifiers(symbol.CustomModifiers, argument))
            {
                return true;
            }

            return false;
        }

        private bool VisitCustomModifiers(ImmutableArray<CustomModifier> customModifiers, TypeParams argument)
        {
            foreach (var customModifier in customModifiers)
            {
                if (Visit((Symbol)customModifier.Modifier, argument))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool VisitAssembly(AssemblySymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitModule(ModuleSymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitEvent(EventSymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitProperty(PropertySymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitField(FieldSymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitParameter(ParameterSymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitLocal(LocalSymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitRangeVariable(RangeVariableSymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitLabel(LabelSymbol symbol, TypeParams argument)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
