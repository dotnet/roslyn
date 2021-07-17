// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class ISymbolExtensions
    {
        public static bool IsType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is ITypeSymbol typeSymbol && typeSymbol.IsType;
        }

        public static bool IsAccessorMethod([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is IMethodSymbol accessorSymbol &&
                (accessorSymbol.IsPropertyAccessor() || accessorSymbol.IsEventAccessor());
        }

        public static IEnumerable<IMethodSymbol> GetAccessors(this ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Property:
                    var property = (IPropertySymbol)symbol;
                    if (property.GetMethod != null)
                    {
                        yield return property.GetMethod;
                    }

                    if (property.SetMethod != null)
                    {
                        yield return property.SetMethod;
                    }

                    break;

                case SymbolKind.Event:
                    var eventSymbol = (IEventSymbol)symbol;
                    if (eventSymbol.AddMethod != null)
                    {
                        yield return eventSymbol.AddMethod;
                    }

                    if (eventSymbol.RemoveMethod != null)
                    {
                        yield return eventSymbol.RemoveMethod;
                    }

                    if (eventSymbol.RaiseMethod != null)
                    {
                        yield return eventSymbol.RaiseMethod;
                    }

                    break;
            }
        }

        public static bool IsDefaultConstructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol.IsConstructor() && symbol.GetParameters().IsEmpty;
        }

        public static bool IsPublic(this ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Public;
        }

        public static bool IsProtected(this ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Protected;
        }

        public static bool IsPrivate(this ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Private;
        }

        public static bool IsErrorType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return
                symbol is ITypeSymbol typeSymbol &&
                typeSymbol.TypeKind == TypeKind.Error;
        }

        public static bool IsConstructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Constructor;
        }

        public static bool IsDestructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.IsFinalizer() ?? false;
        }

        public static bool IsIndexer([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IPropertySymbol)?.IsIndexer == true;
        }

        public static bool IsPropertyWithBackingField([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is IPropertySymbol propertySymbol &&
                propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(f => f.IsImplicitlyDeclared && Equals(f.AssociatedSymbol, symbol));
        }

        /// <summary>
        /// Determines if the given symbol is a backing field for a property.
        /// </summary>
        /// <param name="symbol">This symbol to check.</param>
        /// <param name="propertySymbol">The property that this field symbol is backing.</param>
        /// <returns>True if the given symbol is a backing field for a property, false otherwise.</returns>
        public static bool IsBackingFieldForProperty(
            [NotNullWhen(returnValue: true)] this ISymbol? symbol,
            [NotNullWhen(returnValue: true)] out IPropertySymbol? propertySymbol)
        {
            if (symbol is IFieldSymbol fieldSymbol
                && fieldSymbol.IsImplicitlyDeclared
                && fieldSymbol.AssociatedSymbol is IPropertySymbol p)
            {
                propertySymbol = p;
                return true;
            }
            else
            {
                propertySymbol = null;
                return false;
            }
        }

        public static bool IsUserDefinedOperator([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.UserDefinedOperator;
        }

        public static bool IsConversionOperator([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Conversion;
        }

        public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol? symbol)
        {
            return symbol switch
            {
                IMethodSymbol m => m.Parameters,
                IPropertySymbol p => p.Parameters,
                _ => ImmutableArray.Create<IParameterSymbol>()
            };
        }

        /// <summary>
        /// True if the symbol is externally visible outside this assembly.
        /// </summary>
        public static bool IsExternallyVisible(this ISymbol symbol) =>
            symbol.GetResultantVisibility() == SymbolVisibility.Public;

        public static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
        {
            // Start by assuming it's visible.
            SymbolVisibility visibility = SymbolVisibility.Public;

            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    // Aliases are uber private.  They're only visible in the same file that they
                    // were declared in.
                    return SymbolVisibility.Private;

                case SymbolKind.Parameter:
                    // Parameters are only as visible as their containing symbol
                    return GetResultantVisibility(symbol.ContainingSymbol);

                case SymbolKind.TypeParameter:
                    // Type Parameters are private.
                    return SymbolVisibility.Private;
            }

            while (symbol != null && symbol.Kind != SymbolKind.Namespace)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    // If we see anything private, then the symbol is private.
                    case Accessibility.NotApplicable:
                    case Accessibility.Private:
                        return SymbolVisibility.Private;

                    // If we see anything internal, then knock it down from public to
                    // internal.
                    case Accessibility.Internal:
                    case Accessibility.ProtectedAndInternal:
                        visibility = SymbolVisibility.Internal;
                        break;

                        // For anything else (Public, Protected, ProtectedOrInternal), the
                        // symbol stays at the level we've gotten so far.
                }

                symbol = symbol.ContainingSymbol;
            }

            return visibility;
        }

        public static bool MatchMemberDerivedByName([NotNullWhen(returnValue: true)] this ISymbol? member, INamedTypeSymbol type, string name)
        {
            return member != null && member.MetadataName == name && member.ContainingType.DerivesFrom(type);
        }

        public static bool MatchMethodDerivedByName([NotNullWhen(returnValue: true)] this IMethodSymbol? method, INamedTypeSymbol type, string name)
        {
            return method != null && method.MatchMemberDerivedByName(type, name);
        }

        public static bool MatchMethodByName([NotNullWhen(returnValue: true)] this ISymbol? member, INamedTypeSymbol type, string name)
        {
            return member != null && member.Kind == SymbolKind.Method && member.MatchMemberByName(type, name);
        }

        public static bool MatchPropertyDerivedByName([NotNullWhen(returnValue: true)] this ISymbol? member, INamedTypeSymbol type, string name)
        {
            return member != null && member.Kind == SymbolKind.Property && member.MatchMemberDerivedByName(type, name);
        }

        public static bool MatchMemberByName([NotNullWhen(returnValue: true)] this ISymbol? member, INamedTypeSymbol type, string name)
        {
            return member != null && Equals(member.ContainingType, type) && member.MetadataName == name;
        }

        public static bool MatchPropertyByName([NotNullWhen(returnValue: true)] this ISymbol? member, INamedTypeSymbol type, string name)
        {
            return member != null && member.Kind == SymbolKind.Property && member.MatchMemberByName(type, name);
        }

        public static bool MatchFieldByName([NotNullWhen(returnValue: true)] this ISymbol? member, INamedTypeSymbol type, string name)
        {
            return member != null && member.Kind == SymbolKind.Field && member.MatchMemberByName(type, name);
        }

        // Define the format in for displaying member names. The format is chosen to be consistent
        // consistent with FxCop's display format.
        private static readonly SymbolDisplayFormat s_memberDisplayFormat =
            // This format omits the namespace.
            SymbolDisplayFormat.CSharpShortErrorMessageFormat
                // Turn off the EscapeKeywordIdentifiers flag (which is on by default), so that
                // a method named "@for" is displayed as "for".
                // Turn on the UseSpecialTypes flat (which is off by default), so that parameter
                // names of "special" types such as Int32 are displayed as their language alias,
                // such as int for C# and Integer for VB.
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// Format member names in a way consistent with FxCop's display format.
        /// </summary>
        /// <param name="member"></param>
        /// <returns>
        /// A string representing the name of the member in a format consistent with FxCop.
        /// </returns>
        public static string FormatMemberName(this ISymbol member)
        {
            return member.ToDisplayString(s_memberDisplayFormat);
        }

        /// <summary>
        /// Check whether given parameters contains any parameter with given type.
        /// </summary>
        public static bool ContainsParameterOfType(this IEnumerable<IParameterSymbol> parameters, INamedTypeSymbol type)
        {
            var parametersOfType = GetParametersOfType(parameters, type);
            return parametersOfType.Any();
        }

        /// <summary>
        /// Get parameters which type is the given type
        /// </summary>
        public static IEnumerable<IParameterSymbol> GetParametersOfType(this IEnumerable<IParameterSymbol> parameters, INamedTypeSymbol type)
        {
            return parameters.Where(p => p.Type.Equals(type));
        }

        /// <summary>
        /// Get parameters which type is the given special type
        /// </summary>
        public static IEnumerable<IParameterSymbol> GetParametersOfType(this IEnumerable<IParameterSymbol> parameters, SpecialType specialType)
        {
            return parameters.Where(p => p.Type.SpecialType == specialType));
        }

        /// <summary>
        /// Check whether given overloads has any overload whose parameters has the given type as its parameter type.
        /// </summary>
        public static bool HasOverloadWithParameterOfType(this IEnumerable<IMethodSymbol> overloads, IMethodSymbol self, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            foreach (var overload in overloads)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (self?.Equals(overload) == true)
                {
                    continue;
                }

                if (overload.Parameters.ContainsParameterOfType(type))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Convert given parameters to the indices to the given method's parameter list.
        /// </summary>
        public static IEnumerable<int> GetParameterIndices(this IMethodSymbol method, IEnumerable<IParameterSymbol> parameters, CancellationToken cancellationToken)
        {
            var set = new HashSet<IParameterSymbol>(parameters);
            for (var i = 0; i < method.Parameters.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (set.Contains(method.Parameters[i]))
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Check whether parameter count and parameter types of the given methods are same.
        /// </summary>
        public static bool ParametersAreSame(this IMethodSymbol method1, IMethodSymbol method2)
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
        public static bool ParameterTypesAreSame(this IMethodSymbol method1, IMethodSymbol method2, IEnumerable<int> parameterIndices, CancellationToken cancellationToken)
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

        private static bool ParameterTypesAreSame(this IParameterSymbol parameter1, IParameterSymbol parameter2)
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
            return type2.Equals(type1);
        }

        /// <summary>
        /// Check whether return type, parameters count and parameter types are same for the given methods.
        /// </summary>
        public static bool ReturnTypeAndParametersAreSame(this IMethodSymbol method, IMethodSymbol otherMethod)
            => method.ReturnType.Equals(otherMethod.ReturnType) &&
               method.ParametersAreSame(otherMethod);

        /// <summary>
        /// Check whether given symbol is from mscorlib
        /// </summary>
        public static bool IsFromMscorlib(this ISymbol symbol, Compilation compilation)
        {
            var @object = compilation.GetSpecialType(SpecialType.System_Object);
            return symbol.ContainingAssembly?.Equals(@object.ContainingAssembly) == true;
        }

        /// <summary>
        /// Get overload from the given overloads that matches given method signature + given parameter
        /// </summary>
        public static IMethodSymbol? GetMatchingOverload(this IMethodSymbol method, IEnumerable<IMethodSymbol> overloads, int parameterIndex, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            foreach (IMethodSymbol overload in overloads)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // does not account for method with optional parameters
                if (method.Equals(overload) || overload.Parameters.Length != method.Parameters.Length)
                {
                    // either itself, or signature is not same
                    continue;
                }

                if (!method.ParameterTypesAreSame(overload, Enumerable.Range(0, method.Parameters.Length).Where(i => i != parameterIndex), cancellationToken))
                {
                    // check whether remaining parameters match existing types, otherwise, we are not interested
                    continue;
                }

                if (overload.Parameters[parameterIndex].Type.Equals(type))
                {
                    // we no longer interested in this overload. there can be only 1 match
                    return overload;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a given symbol implements an interface member implicitly or explicitly
        /// </summary>
        public static bool IsImplementationOfAnyInterfaceMember(this ISymbol symbol)
        {
            return symbol.IsImplementationOfAnyExplicitInterfaceMember() || symbol.IsImplementationOfAnyImplicitInterfaceMember();
        }

        public static bool IsImplementationOfAnyImplicitInterfaceMember(this ISymbol symbol)
        {
            return IsImplementationOfAnyImplicitInterfaceMember<ISymbol>(symbol);
        }

        /// <summary>
        /// Checks if a given symbol implements an interface member implicitly
        /// </summary>
        public static bool IsImplementationOfAnyImplicitInterfaceMember<TSymbol>(this ISymbol symbol)
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

        public static bool IsImplementationOfInterfaceMember(this ISymbol symbol, [NotNullWhen(returnValue: true)] ISymbol? interfaceMember)
        {
            return interfaceMember != null &&
                   symbol.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember));
        }

        /// <summary>
        /// Checks if a given symbol implements an interface member or overrides an implementation of an interface member.
        /// </summary>
        public static bool IsOverrideOrImplementationOfInterfaceMember(this ISymbol symbol, [NotNullWhen(returnValue: true)] ISymbol? interfaceMember)
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

        /// <summary>
        /// Gets the symbol overridden by the given <paramref name="symbol"/>.
        /// </summary>
        /// <remarks>Requires that <see cref="ISymbol.IsOverride"/> is true for the given <paramref name="symbol"/>.</remarks>
        public static ISymbol GetOverriddenMember(this ISymbol symbol)
        {
            Debug.Assert(symbol.IsOverride);

            return symbol switch
            {
                IMethodSymbol methodSymbol => methodSymbol.OverriddenMethod,

                IPropertySymbol propertySymbol => propertySymbol.OverriddenProperty,

                IEventSymbol eventSymbol => eventSymbol.OverriddenEvent,

                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Checks if a given symbol implements an interface member explicitly
        /// </summary>
        public static bool IsImplementationOfAnyExplicitInterfaceMember([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            if (symbol is IMethodSymbol methodSymbol && methodSymbol.ExplicitInterfaceImplementations.Any())
            {
                return true;
            }

            if (symbol is IPropertySymbol propertySymbol && propertySymbol.ExplicitInterfaceImplementations.Any())
            {
                return true;
            }

            if (symbol is IEventSymbol eventSymbol && eventSymbol.ExplicitInterfaceImplementations.Any())
            {
                return true;
            }

            return false;
        }

        public static ITypeSymbol? GetMemberOrLocalOrParameterType(this ISymbol symbol)
        {
            return symbol.Kind switch
            {
                SymbolKind.Local => ((ILocalSymbol)symbol).Type,

                SymbolKind.Parameter => ((IParameterSymbol)symbol).Type,

                _ => GetMemberType(symbol),
            };
        }

        public static ITypeSymbol? GetMemberType(this ISymbol? symbol)
        {
            return symbol switch
            {
                IEventSymbol eventSymbol => eventSymbol.Type,

                IFieldSymbol fieldSymbol => fieldSymbol.Type,

                IMethodSymbol methodSymbol => methodSymbol.ReturnType,

                IPropertySymbol propertySymbol => propertySymbol.Type,

                _ => null,
            };
        }

        public static bool IsReadOnlyFieldOrProperty([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol switch
            {
                IFieldSymbol field => field.IsReadOnly,

                IPropertySymbol property => property.IsReadOnly,

                _ => false,
            };
        }

        /// <summary>
        /// Returns a value indicating whether the specified symbol has the specified
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
        /// <remarks>
        /// If <paramref name="symbol"/> is a type, this method does not find attributes
        /// on its base types.
        /// </remarks>
        public static bool HasAttribute(this ISymbol symbol, [NotNullWhen(returnValue: true)] INamedTypeSymbol? attribute)
        {
            return attribute != null && symbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(attribute));
        }

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
        public static bool HasDerivedTypeAttribute(this ITypeSymbol symbol, [NotNullWhen(returnValue: true)] INamedTypeSymbol? attribute)
        {
            if (attribute == null)
            {
                return false;
            }

            while (symbol != null)
            {
                if (symbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(attribute)))
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
        public static bool HasDerivedMethodAttribute(this IMethodSymbol symbol, [NotNullWhen(returnValue: true)] INamedTypeSymbol? attribute)
        {
            if (attribute == null)
            {
                return false;
            }

            while (symbol != null)
            {
                if (symbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(attribute)))
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

        /// <summary>
        /// Determines if the given symbol has the specified attributes.
        /// </summary>
        /// <param name="symbol">Symbol to examine.</param>
        /// <param name="attributes">Type symbols of the attributes to check for.</param>
        /// <returns>Boolean array, same size and order as <paramref name="attributes"/>, indicating that the corresponding
        /// attribute is present.</returns>
        public static bool[] HasAttributes(this ISymbol symbol, params INamedTypeSymbol?[] attributes)
        {
            bool[] isAttributePresent = new bool[attributes.Length];
            foreach (var attributeData in symbol.GetAttributes())
            {
                for (int i = 0; i < attributes.Length; i++)
                {
                    if (attributeData.AttributeClass.Equals(attributes[i]))
                    {
                        isAttributePresent[i] = true;
                    }
                }
            }

            return isAttributePresent;
        }

        /// <summary>
        /// Gets enumeration of attributes that are of the specified type.
        /// </summary>
        /// <param name="symbol">This symbol whose attributes to get.</param>
        /// <param name="attributeType">Type of attribute to look for.</param>
        /// <returns>Enumeration of attributes.</returns>
        [SuppressMessage("RoslyDiagnosticsPerformance", "RS0001:Use SpecializedCollections.EmptyEnumerable()", Justification = "Not available in all projects")]
        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, INamedTypeSymbol? attributeType)
        {
            if (attributeType == null)
            {
                return Enumerable.Empty<AttributeData>();
            }

            return symbol.GetAttributes().Where(attr => attr.AttributeClass.Equals(attributeType));
        }

        /// <summary>
        /// Indicates if a symbol has at least one location in source.
        /// </summary>
        public static bool IsInSource(this ISymbol symbol)
        {
            return symbol.Locations.Any(l => l.IsInSource);
        }

        public static bool IsLambdaOrLocalFunction([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as IMethodSymbol)?.IsLambdaOrLocalFunction() == true;

        /// <summary>
        /// Returns true for symbols whose name starts with an underscore and
        /// are optionally followed by an integer, such as '_', '_1', '_2', etc.
        /// These symbols can be treated as special discard symbol names.
        /// </summary>
        public static bool IsSymbolWithSpecialDiscardName([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol?.Name.StartsWith("_", StringComparison.Ordinal) == true &&
               (symbol.Name.Length == 1 || uint.TryParse(symbol.Name[1..], out _));

        public static bool IsConst([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol switch
            {
                IFieldSymbol field => field.IsConst,

                ILocalSymbol local => local.IsConst,

                _ => false,
            };
        }

        public static bool IsReadOnly([NotNullWhen(returnValue: true)] this ISymbol? symbol)
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
}
