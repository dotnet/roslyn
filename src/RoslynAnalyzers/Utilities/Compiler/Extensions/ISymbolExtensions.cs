// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
// using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Analyzer.Utilities.Extensions
{
    internal static class ISymbolExtensions
    {
        extension([NotNullWhen(true)] ISymbol? symbol)
        {
            public bool IsDefaultConstructor()
            {
                return symbol.IsConstructor() && symbol.GetParameters().IsEmpty;
            }

            public bool IsIndexer()
            {
                return symbol is IPropertySymbol { IsIndexer: true };
            }

            public bool IsPropertyWithBackingField([NotNullWhen(true)] out IFieldSymbol? backingField)
            {
                if (symbol is IPropertySymbol propertySymbol)
                {
                    foreach (ISymbol member in propertySymbol.ContainingType.GetMembers())
                    {
                        if (member is IFieldSymbol associated &&
                            associated.IsImplicitlyDeclared &&
                            Equals(associated.AssociatedSymbol, propertySymbol))
                        {
                            backingField = associated;
                            return true;
                        }
                    }
                }

                backingField = null;
                return false;
            }

            /// <summary>
            /// Checks if a given symbol implements an interface member explicitly
            /// </summary>
            public bool IsImplementationOfAnyExplicitInterfaceMember()
            {
                if (symbol is IMethodSymbol methodSymbol && !methodSymbol.ExplicitInterfaceImplementations.IsEmpty)
                {
                    return true;
                }

                if (symbol is IPropertySymbol propertySymbol && !propertySymbol.ExplicitInterfaceImplementations.IsEmpty)
                {
                    return true;
                }

                if (symbol is IEventSymbol eventSymbol && !eventSymbol.ExplicitInterfaceImplementations.IsEmpty)
                {
                    return true;
                }

                return false;
            }

            public bool IsReadOnlyFieldOrProperty()
            {
                return symbol switch
                {
                    IFieldSymbol field => field.IsReadOnly,

                    IPropertySymbol property => property.IsReadOnly,

                    _ => false,
                };
            }

            public bool IsLambdaOrLocalFunction()
                => (symbol as IMethodSymbol)?.IsLambdaOrLocalFunction() == true;

            public bool IsConst()
            {
                return symbol switch
                {
                    IFieldSymbol field => field.IsConst,

                    ILocalSymbol local => local.IsConst,

                    _ => false,
                };
            }

            public bool IsReadOnly()
                => symbol switch
                {
                    IFieldSymbol field => field.IsReadOnly,
                    IPropertySymbol property => property.IsReadOnly,
#if CODEANALYSIS_V3_OR_BETTER
                    IMethodSymbol method => method.IsReadOnly,
                    ITypeSymbol type => type.IsReadOnly,
#endif
                    _ => false,
                };
        }

        extension(ISymbol symbol)
        {
            public bool IsPublic()
            {
                return symbol.DeclaredAccessibility == Accessibility.Public;
            }

            public bool IsPrivate()
            {
                return symbol.DeclaredAccessibility == Accessibility.Private;
            }

            /// <summary>
            /// True if the symbol is externally visible outside this assembly.
            /// </summary>
            public bool IsExternallyVisible() =>
                symbol.GetResultantVisibility() == SymbolVisibility.Public;

            /// <summary>
            /// Checks if a given symbol implements an interface member implicitly or explicitly
            /// </summary>
            public bool IsImplementationOfAnyInterfaceMember()
            {
                return symbol.IsImplementationOfAnyExplicitInterfaceMember() || symbol.IsImplementationOfAnyImplicitInterfaceMember();
            }

            public bool IsImplementationOfAnyImplicitInterfaceMember()
            {
                return IsImplementationOfAnyImplicitInterfaceMember<ISymbol>(symbol);
            }

            /// <summary>
            /// Checks if a given symbol implements an interface member implicitly
            /// </summary>
            public bool IsImplementationOfAnyImplicitInterfaceMember<TSymbol>()
                where TSymbol : ISymbol
            {
                if (symbol.ContainingType != null)
                {
                    foreach (INamedTypeSymbol interfaceSymbol in symbol.ContainingType.AllInterfaces)
                    {
                        foreach (var interfaceMember in interfaceSymbol.GetMembers().OfType<TSymbol>())
                        {
                            if (IsImplementationOfInterfaceMember(symbol, interfaceMember))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            public bool IsImplementationOfInterfaceMember([NotNullWhen(returnValue: true)] ISymbol? interfaceMember)
            {
                return interfaceMember != null &&
                    SymbolEqualityComparer.Default.Equals(symbol, symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember));
            }

            /// <summary>
            /// Checks if a given symbol implements an interface member or overrides an implementation of an interface member.
            /// </summary>
            public bool IsOverrideOrImplementationOfInterfaceMember([NotNullWhen(returnValue: true)] ISymbol? interfaceMember)
            {
                if (interfaceMember == null)
                {
                    return false;
                }

                if (symbol.IsImplementationOfInterfaceMember(interfaceMember))
                {
                    return true;
                }

                return symbol.IsOverride &&
                    symbol.GetOverriddenMember()?.IsOverrideOrImplementationOfInterfaceMember(interfaceMember) == true;
            }

            public ITypeSymbol? GetMemberOrLocalOrParameterType()
            {
                return symbol.Kind switch
                {
                    SymbolKind.Local => ((ILocalSymbol)symbol).Type,

                    SymbolKind.Parameter => ((IParameterSymbol)symbol).Type,

                    _ => symbol.GetMemberType(),
                };
            }

            public AttributeData? GetAttribute([NotNullWhen(true)] INamedTypeSymbol? attributeType)
            {
                return symbol.GetAttributes(attributeType).FirstOrDefault();
            }

            public IEnumerable<AttributeData> GetAttributes(IEnumerable<INamedTypeSymbol?> attributesToMatch)
            {
                foreach (var attribute in symbol.GetAttributes())
                {
                    if (attribute.AttributeClass == null)
                        continue;

                    foreach (var attributeToMatch in attributesToMatch)
                    {
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeToMatch))
                        {
                            yield return attribute;
                            break;
                        }
                    }
                }
            }

            public IEnumerable<AttributeData> GetAttributes(params INamedTypeSymbol?[] attributeTypesToMatch)
            {
                return symbol.GetAttributes(attributesToMatch: attributeTypesToMatch);
            }

            public bool HasAnyAttribute(IEnumerable<INamedTypeSymbol> attributesToMatch)
            {
                return symbol.GetAttributes(attributesToMatch).Any();
            }

            public bool HasAnyAttribute(params INamedTypeSymbol?[] attributeTypesToMatch)
            {
                return symbol.GetAttributes(attributeTypesToMatch).Any();
            }
        }

        extension(IMethodSymbol method1)
        {
            /// <summary>
            /// Check whether parameter count and parameter types of the given methods are same.
            /// </summary>
            public bool ParametersAreSame(IMethodSymbol method2)
            {
                if (method1.Parameters.Length != method2.Parameters.Length)
                {
                    return false;
                }

                for (int index = 0; index < method1.Parameters.Length; index++)
                {
                    if (!ParameterTypesAreSame(method1.Parameters[index], method2.Parameters[index]))
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Check whether parameter types of the given methods are same for given parameter indices.
            /// </summary>
            public bool ParameterTypesAreSame(IMethodSymbol method2, IEnumerable<int> parameterIndices, CancellationToken cancellationToken)
            {
                foreach (int index in parameterIndices)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!ParameterTypesAreSame(method1.Parameters[index], method2.Parameters[index]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        extension(IParameterSymbol parameter1)
        {
            public bool ParameterTypesAreSame(IParameterSymbol parameter2)
            {
                var type1 = parameter1.Type.OriginalDefinition;
                var type2 = parameter2.Type.OriginalDefinition;

                if (type1.TypeKind == TypeKind.TypeParameter &&
                    type2.TypeKind == TypeKind.TypeParameter &&
                    ((ITypeParameterSymbol)type1).Ordinal == ((ITypeParameterSymbol)type2).Ordinal)
                {
                    return true;
                }

                // this doesn't account for type conversion but FxCop implementation seems doesn't either
                // so this should match FxCop implementation.
                return SymbolEqualityComparer.Default.Equals(type2, type1);
            }
        }

        extension(IMethodSymbol method)
        {
            /// <summary>
            /// Check whether return type, parameters count and parameter types are same for the given methods.
            /// </summary>
            public bool ReturnTypeAndParametersAreSame(IMethodSymbol otherMethod)
                => SymbolEqualityComparer.Default.Equals(method.ReturnType, otherMethod.ReturnType) &&
                   method.ParametersAreSame(otherMethod);
        }

        extension(ITypeSymbol symbol)
        {
            /// <summary>
            /// Returns a value indicating whether the specified or inherited symbol has the specified
            /// attribute.
            /// </summary>
            /// <param name="symbol">
            /// The symbol being examined.
            /// </param>
            /// <param name="attribute">
            /// The attribute in question.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="symbol"/> has an attribute of type
            /// <paramref name="attribute"/>; otherwise <see langword="false"/>.
            /// </returns>
            public bool HasDerivedTypeAttribute([NotNullWhen(returnValue: true)] INamedTypeSymbol? attribute)
            {
                if (attribute == null)
                {
                    return false;
                }

                while (symbol != null)
                {
                    if (symbol.HasAnyAttribute(attribute))
                    {
                        return true;
                    }

                    if (symbol.BaseType == null)
                    {
                        return false;
                    }

                    symbol = symbol.BaseType;
                }

                return false;
            }
        }

        extension(IMethodSymbol symbol)
        {
            /// <summary>
            /// Returns a value indicating whether the specified or inherited method symbol has the specified
            /// attribute.
            /// </summary>
            /// <param name="symbol">
            /// The symbol being examined.
            /// </param>
            /// <param name="attribute">
            /// The attribute in question.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="symbol"/> has an attribute of type
            /// <paramref name="attribute"/>; otherwise <see langword="false"/>.
            /// </returns>
            public bool HasDerivedMethodAttribute([NotNullWhen(returnValue: true)] INamedTypeSymbol? attribute)
            {
                if (attribute == null)
                {
                    return false;
                }

                while (symbol != null)
                {
                    if (symbol.HasAnyAttribute(attribute))
                    {
                        return true;
                    }

                    if (symbol.OverriddenMethod == null)
                    {
                        return false;
                    }

                    symbol = symbol.OverriddenMethod;
                }

                return false;
            }
        }
    }
}
