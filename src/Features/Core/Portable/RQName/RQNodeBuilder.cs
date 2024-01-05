// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static RQNode? Build(ISymbol symbol)
            => symbol switch
            {
                INamespaceSymbol namespaceSymbol => BuildNamespace(namespaceSymbol),
                INamedTypeSymbol namedTypeSymbol => BuildUnconstructedNamedType(namedTypeSymbol),
                IMethodSymbol methodSymbol => BuildMethod(methodSymbol),
                IFieldSymbol fieldSymbol => BuildField(fieldSymbol),
                IEventSymbol eventSymbol => BuildEvent(eventSymbol),
                IPropertySymbol propertySymbol => BuildProperty(propertySymbol),
                _ => null,
            };

        private static RQNamespace BuildNamespace(INamespaceSymbol @namespace)
            => new(RQNodeBuilder.GetNameParts(@namespace));

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

        private static RQUnconstructedType? BuildUnconstructedNamedType(INamedTypeSymbol type)
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

        private static RQMember? BuildField(IFieldSymbol symbol)
        {
            var containingType = BuildUnconstructedNamedType(symbol.ContainingType);

            if (containingType == null)
            {
                return null;
            }

            return new RQMemberVariable(containingType, symbol.Name);
        }

        private static RQProperty? BuildProperty(IPropertySymbol symbol)
        {
            RQMethodPropertyOrEventName name = symbol.IsIndexer
                ? RQOrdinaryMethodPropertyOrEventName.CreateOrdinaryIndexerName()
                : RQOrdinaryMethodPropertyOrEventName.CreateOrdinaryPropertyName(symbol.Name);

            if (symbol.ExplicitInterfaceImplementations.Any())
            {
                if (symbol.ExplicitInterfaceImplementations.Length > 1)
                {
                    return null;
                }

                var interfaceType = BuildType(symbol.ExplicitInterfaceImplementations.Single().ContainingType);

                if (interfaceType != null)
                {
                    name = new RQExplicitInterfaceMemberName(
                        interfaceType,
                        (RQOrdinaryMethodPropertyOrEventName)name);
                }
            }

            var containingType = BuildUnconstructedNamedType(symbol.ContainingType);

            if (containingType == null)
            {
                return null;
            }

            var parameterList = BuildParameterList(symbol.Parameters);

            if (parameterList == null)
            {
                return null;
            }

            return new RQProperty(containingType, name, typeParameterCount: 0, parameters: parameterList);
        }

        private static IList<RQParameter>? BuildParameterList(ImmutableArray<IParameterSymbol> parameters)
        {
            var parameterList = new List<RQParameter>();

            foreach (var parameter in parameters)
            {
                var parameterType = BuildType(parameter.Type);

                if (parameterType == null)
                {
                    return null;
                }

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

        private static RQEvent? BuildEvent(IEventSymbol symbol)
        {
            var containingType = BuildUnconstructedNamedType(symbol.ContainingType);

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

                var interfaceType = BuildType(symbol.ExplicitInterfaceImplementations.Single().ContainingType);

                if (interfaceType != null)
                {
                    name = new RQExplicitInterfaceMemberName(interfaceType, (RQOrdinaryMethodPropertyOrEventName)name);
                }
            }

            return new RQEvent(containingType, name);
        }

        private static RQMethod? BuildMethod(IMethodSymbol symbol)
        {
            if (symbol.MethodKind is MethodKind.UserDefinedOperator or
                MethodKind.BuiltinOperator or
                MethodKind.EventAdd or
                MethodKind.EventRemove or
                MethodKind.PropertySet or
                MethodKind.PropertyGet)
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

                var interfaceType = BuildType(symbol.ExplicitInterfaceImplementations.Single().ContainingType);

                if (interfaceType != null)
                {
                    name = new RQExplicitInterfaceMemberName(interfaceType, (RQOrdinaryMethodPropertyOrEventName)name);
                }
            }

            var containingType = BuildUnconstructedNamedType(symbol.ContainingType);

            if (containingType == null)
            {
                return null;
            }

            var typeParamCount = symbol.TypeParameters.Length;
            var parameterList = BuildParameterList(symbol.Parameters);

            if (parameterList == null)
            {
                return null;
            }

            return new RQMethod(containingType, name, typeParamCount, parameterList);
        }

        private static RQType? BuildType(ITypeSymbol symbol)
        {
            if (symbol.IsAnonymousType)
            {
                return null;
            }

            if (symbol.SpecialType == SpecialType.System_Void)
            {
                return RQVoidType.Singleton;
            }
            else if (symbol is IPointerTypeSymbol pointerType)
            {
                var pointedAtType = BuildType(pointerType.PointedAtType);
                if (pointedAtType == null)
                {
                    return null;
                }

                return new RQPointerType(pointedAtType);
            }
            else if (symbol is IArrayTypeSymbol arrayType)
            {
                var elementType = BuildType(arrayType.ElementType);
                if (elementType == null)
                {
                    return null;
                }

                return new RQArrayType(arrayType.Rank, elementType);
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
            else if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                var definingType = namedTypeSymbol.ConstructedFrom ?? namedTypeSymbol;

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
                        var rqType = BuildType(typeArgument);
                        if (rqType == null)
                        {
                            return null;
                        }

                        typeArgumentList.Add(rqType);
                    }
                }

                var containingType = BuildUnconstructedNamedType(definingType);

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
