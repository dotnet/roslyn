// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Features.RQName.Nodes;

namespace Microsoft.CodeAnalysis.Features.RQName
{
    internal static class RQNodeBuilder
    {
        /// <summary>
        /// Builds the RQName for a given symbol.
        /// </summary>
        /// <returns>The node if it could be created, otherwise null</returns>
        public static UnresolvedRQNode Build(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                    return BuildNamespace(symbol as INamespaceSymbol);
                case SymbolKind.NamedType:
                    return BuildNamedType(symbol as INamedTypeSymbol);
                case SymbolKind.Method:
                    return BuildMethod(symbol as IMethodSymbol);
                case SymbolKind.Field:
                    return BuildField(symbol as IFieldSymbol);
                case SymbolKind.Event:
                    return BuildEvent(symbol as IEventSymbol);
                case SymbolKind.Property:
                    return BuildProperty(symbol as IPropertySymbol);
                default:
                    return null;
            }
        }

        private static RQNamespace BuildNamespace(INamespaceSymbol @namespace)
        {
            return new RQNamespace(RQNodeBuilder.GetNameParts(@namespace));
        }

        private static IList<string> GetNameParts(INamespaceSymbol @namespace)
        {
            var parts = new List<string>();

            if (@namespace == null)
            {
                return parts;
            }

            while (!@namespace.IsGlobalNamespace)
            {
                parts.Add(@namespace.Name);
                @namespace = @namespace.ContainingNamespace;
            }

            parts.Reverse();
            return parts;
        }

        private static RQUnconstructedType BuildNamedType(INamedTypeSymbol type)
        {
            // Anything that is a valid RQUnconstructed types is ALWAYS safe for public APIs

            if (type == null)
            {
                return null;
            }

            // Anonymous types are unsupported
            if (type.IsAnonymousType)
            {
                return null;
            }

            // the following types are supported for BuildType() used in signatures, but are not supported
            // for UnconstructedTypes
            if (type != type.ConstructedFrom || type.SpecialType == SpecialType.System_Void)
            {
                return null;
            }

            // make an RQUnconstructedType
            var namespaceNames = RQNodeBuilder.GetNameParts(@type.ContainingNamespace);
            var typeInfos = new List<RQUnconstructedTypeInfo>();

            for (var currentType = type; currentType != null; currentType = currentType.ContainingType)
            {
                typeInfos.Insert(0, new RQUnconstructedTypeInfo(currentType.Name, currentType.TypeParameters.Length));
            }

            return new RQUnconstructedType(namespaceNames, typeInfos);
        }

        private static RQMember BuildField(IFieldSymbol symbol)
        {
            var containingType = BuildNamedType(symbol.ContainingType);

            if (containingType == null)
            {
                return null;
            }

            return new RQMemberVariable(containingType, symbol.Name);
        }

        private static RQProperty BuildProperty(IPropertySymbol symbol)
        {
            RQMethodPropertyOrEventName name = symbol.IsIndexer ?
                RQOrdinaryMethodPropertyOrEventName.CreateOrdinaryIndexerName() :
                RQOrdinaryMethodPropertyOrEventName.CreateOrdinaryPropertyName(symbol.Name);

            if (symbol.ExplicitInterfaceImplementations.Any())
            {
                if (symbol.ExplicitInterfaceImplementations.Length > 1)
                {
                    return null;
                }

                name = new RQExplicitInterfaceMemberName(
                    BuildType(symbol.ExplicitInterfaceImplementations.Single().ContainingType as ITypeSymbol),
                    (RQOrdinaryMethodPropertyOrEventName)name);
            }

            var containingType = BuildNamedType(symbol.ContainingType);

            if (containingType == null)
            {
                return null;
            }

            var parameterList = BuildParameterList(symbol.Parameters);

            return new RQProperty(containingType, name, typeParameterCount: 0, parameters: parameterList);
        }

        private static IList<RQParameter> BuildParameterList(ImmutableArray<IParameterSymbol> parameters)
        {
            var parameterList = new List<RQParameter>();

            foreach (var parameter in parameters)
            {
                var parameterType = BuildType(parameter.Type);

                if (parameter.RefKind == RefKind.Out)
                {
                    parameterList.Add(new RQOutParameter(parameterType));
                }
                else if (parameter.RefKind == RefKind.Ref)
                {
                    parameterList.Add(new RQRefParameter(parameterType));
                }
                else
                {
                    parameterList.Add(new RQNormalParameter(parameterType));
                }
            }

            return parameterList;
        }

        private static RQEvent BuildEvent(IEventSymbol symbol)
        {
            var containingType = BuildNamedType(symbol.ContainingType);

            if (containingType == null)
            {
                return null;
            }

            RQMethodPropertyOrEventName name = RQOrdinaryMethodPropertyOrEventName.CreateOrdinaryEventName(symbol.Name);

            if (symbol.ExplicitInterfaceImplementations.Any())
            {
                if (symbol.ExplicitInterfaceImplementations.Length > 1)
                {
                    return null;
                }

                name = new RQExplicitInterfaceMemberName(BuildType(symbol.ExplicitInterfaceImplementations.Single().ContainingType as ITypeSymbol), (RQOrdinaryMethodPropertyOrEventName)name);
            }

            return new RQEvent(containingType, name);
        }

        private static RQMethod BuildMethod(IMethodSymbol symbol)
        {
            if (symbol.MethodKind == MethodKind.UserDefinedOperator ||
                symbol.MethodKind == MethodKind.BuiltinOperator ||
                symbol.MethodKind == MethodKind.EventAdd ||
                symbol.MethodKind == MethodKind.EventRemove ||
                symbol.MethodKind == MethodKind.PropertySet ||
                symbol.MethodKind == MethodKind.PropertyGet)
            {
                return null;
            }

            RQMethodPropertyOrEventName name;

            if (symbol.MethodKind == MethodKind.Constructor)
            {
                name = RQOrdinaryMethodPropertyOrEventName.CreateConstructorName();
            }
            else if (symbol.MethodKind == MethodKind.Destructor)
            {
                name = RQOrdinaryMethodPropertyOrEventName.CreateDestructorName();
            }
            else
            {
                name = RQOrdinaryMethodPropertyOrEventName.CreateOrdinaryMethodName(symbol.Name);
            }

            if (symbol.ExplicitInterfaceImplementations.Any())
            {
                if (symbol.ExplicitInterfaceImplementations.Length > 1)
                {
                    return null;
                }

                name = new RQExplicitInterfaceMemberName(BuildType(symbol.ExplicitInterfaceImplementations.Single().ContainingType as ITypeSymbol), (RQOrdinaryMethodPropertyOrEventName)name);
            }

            var containingType = BuildNamedType(symbol.ContainingType);

            if (containingType == null)
            {
                return null;
            }

            var typeParamCount = symbol.TypeParameters.Length;
            var parameterList = BuildParameterList(symbol.Parameters);

            return new RQMethod(containingType, name, typeParamCount, parameterList);
        }

        private static RQType BuildType(ITypeSymbol symbol)
        {
            if (symbol.IsAnonymousType)
            {
                return null;
            }

            if (symbol.SpecialType == SpecialType.System_Void)
            {
                return RQVoidType.Singleton;
            }
            else if (symbol.TypeKind == TypeKind.Pointer)
            {
                return new RQPointerType(BuildType((symbol as IPointerTypeSymbol).PointedAtType));
            }
            else if (symbol.TypeKind == TypeKind.Array)
            {
                return new RQArrayType((symbol as IArrayTypeSymbol).Rank, BuildType((symbol as IArrayTypeSymbol).ElementType));
            }
            else if (symbol.TypeKind == TypeKind.TypeParameter)
            {
                return new RQTypeVariableType(symbol.Name);
            }
            else if (symbol.TypeKind == TypeKind.Unknown)
            {
                return new RQErrorType(symbol.Name);
            }
            else if (symbol.TypeKind == TypeKind.Dynamic)
            {
                // NOTE: Because RQNames were defined as an interchange format before C# had "dynamic", and we didn't want 
                // all consumers to have to update their logic to crack the attributes about whether something is object or
                // not, we just erase dynamic to object here.
                return RQType.ObjectType;
            }
            else if (symbol.Kind == SymbolKind.NamedType || symbol.Kind == SymbolKind.ErrorType)
            {
                var namedTypeSymbol = symbol as INamedTypeSymbol;

                var definingType = namedTypeSymbol.ConstructedFrom != null ? namedTypeSymbol.ConstructedFrom : namedTypeSymbol;

                var typeChain = new List<INamedTypeSymbol>();
                var type = namedTypeSymbol;
                typeChain.Add(namedTypeSymbol);

                while (type.ContainingType != null)
                {
                    type = type.ContainingType;
                    typeChain.Add(type);
                }

                typeChain.Reverse();

                var typeArgumentList = new List<RQType>();

                foreach (var entry in typeChain)
                {
                    foreach (var typeArgument in entry.TypeArguments)
                    {
                        typeArgumentList.Add(BuildType(typeArgument));
                    }
                }

                var containingType = BuildNamedType(definingType);

                if (containingType == null)
                {
                    return null;
                }

                return new RQConstructedType(containingType, typeArgumentList);
            }
            else
            {
                return null;
            }
        }
    }
}
