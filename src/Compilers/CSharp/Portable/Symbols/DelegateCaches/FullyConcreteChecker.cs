// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Checks whether a type or a method is fully constructed with fully concrete types as opposed to yet another generic parameter.
    /// </summary>
    internal class FullyConcreteChecker : CSharpSymbolVisitor<bool>
    {
        public static FullyConcreteChecker Instance { get; } = new FullyConcreteChecker();

        public override bool VisitTypeParameter(TypeParameterSymbol symbol) => false;

        public override bool VisitAlias(AliasSymbol symbol) => true;

        public override bool VisitArrayType(ArrayTypeSymbol symbol)
        {
            if (!Visit(symbol.ElementType))
            {
                return false;
            }

            if (!VisitCustomModifiers(symbol.CustomModifiers))
            {
                return false;
            }

            return true;
        }

        public override bool VisitNamedType(NamedTypeSymbol symbol)
        {
            if (symbol.IsAnonymousType)
            {
                foreach (var anonPropType in AnonymousTypeManager.GetAnonymousTypePropertyTypes(symbol))
                {
                    if (!Visit(anonPropType))
                    {
                        return false;
                    }
                }

                return true;
            }

            foreach (var typeArg in symbol.TypeArguments)
            {
                if (!Visit(typeArg))
                {
                    return false;
                }
            }

            if (symbol.HasTypeArgumentsCustomModifiers)
            {
                var count = symbol.TypeArgumentsNoUseSiteDiagnostics.Length;
                for (int i = 0; i < count; i++)
                {
                    if (!VisitCustomModifiers(symbol.GetTypeArgumentCustomModifiers(i)))
                    {
                        return false;
                    }
                }
            }

            if ((object)symbol.ContainingType != null && !symbol.ContainingType.Accept(this))
            {
                return false;
            }

            return true;
        }

        public override bool VisitMethod(MethodSymbol symbol)
        {
            foreach (var typeArg in symbol.TypeArguments)
            {
                if (!Visit(typeArg))
                {
                    return false;
                }
            }

            if ((object)symbol.ContainingType != null && !symbol.ContainingType.Accept(this))
            {
                return false;
            }

            return true;
        }

        public override bool VisitDynamicType(DynamicTypeSymbol symbol) => true;

        public override bool VisitPointerType(PointerTypeSymbol symbol)
        {
            // Func<int*[]> is a good example why here is reachable.

            // Although PointedAtType cannot be generic by code, we don't know what can come from metadata.
            if (!Visit(symbol.PointedAtType))
            {
                return false;
            }

            if (!VisitCustomModifiers(symbol.CustomModifiers))
            {
                return false;
            }

            return true;
        }

        private bool VisitCustomModifiers(ImmutableArray<CustomModifier> customModifiers)
        {
            foreach (var customModifier in customModifiers)
            {
                if (!Visit((Symbol)customModifier.Modifier))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool VisitNamespace(NamespaceSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitAssembly(AssemblySymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitModule(ModuleSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitEvent(EventSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitProperty(PropertySymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitField(FieldSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitParameter(ParameterSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitLocal(LocalSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitRangeVariable(RangeVariableSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool VisitLabel(LabelSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
