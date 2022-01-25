﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class IMethodSymbolExtensions
    {
        public static bool CompatibleSignatureToDelegate(this IMethodSymbol method, INamedTypeSymbol delegateType)
        {
            Contract.ThrowIfFalse(delegateType.TypeKind == TypeKind.Delegate);

            var invoke = delegateType.DelegateInvokeMethod;
            if (invoke == null)
            {
                // It's possible to get events with no invoke method from metadata.  We will assume
                // that no method can be an event handler for one.
                return false;
            }

            if (method.Parameters.Length != invoke.Parameters.Length)
            {
                return false;
            }

            if (method.ReturnsVoid != invoke.ReturnsVoid)
            {
                return false;
            }

            if (!method.ReturnType.InheritsFromOrEquals(invoke.ReturnType))
            {
                return false;
            }

            for (var i = 0; i < method.Parameters.Length; i++)
            {
                if (!invoke.Parameters[i].Type.InheritsFromOrEquals(method.Parameters[i].Type))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the methodSymbol and any partial parts.
        /// </summary>
        public static ImmutableArray<IMethodSymbol> GetAllMethodSymbolsOfPartialParts(this IMethodSymbol method)
        {
            if (method.PartialDefinitionPart != null)
            {
                Debug.Assert(method.PartialImplementationPart == null && !Equals(method.PartialDefinitionPart, method));
                return ImmutableArray.Create(method, method.PartialDefinitionPart);
            }
            else if (method.PartialImplementationPart != null)
            {
                Debug.Assert(!Equals(method.PartialImplementationPart, method));
                return ImmutableArray.Create(method.PartialImplementationPart, method);
            }
            else
            {
                return ImmutableArray.Create(method);
            }
        }

        public static IMethodSymbol RenameTypeParameters(this IMethodSymbol method, ImmutableArray<string> newNames)
        {
            if (method.TypeParameters.Select(t => t.Name).SequenceEqual(newNames))
            {
                return method;
            }

            var typeGenerator = new TypeGenerator();
            var updatedTypeParameters = RenameTypeParameters(
                method.TypeParameters, newNames, typeGenerator);

            var mapping = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            for (var i = 0; i < method.TypeParameters.Length; i++)
            {
                mapping[method.TypeParameters[i]] = updatedTypeParameters[i];
            }

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                method.ContainingType,
                method.GetAttributes(),
                method.DeclaredAccessibility,
                method.GetSymbolModifiers(),
                method.ReturnType.SubstituteTypes(mapping, typeGenerator),
                method.RefKind,
                method.ExplicitInterfaceImplementations,
                method.Name,
                updatedTypeParameters,
                method.Parameters.SelectAsArray(p =>
                    CodeGenerationSymbolFactory.CreateParameterSymbol(p.GetAttributes(), p.RefKind, p.IsParams, p.Type.SubstituteTypes(mapping, typeGenerator), p.Name, p.IsOptional,
                        p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null)));
        }

        public static IMethodSymbol RenameParameters(
            this IMethodSymbol method, ImmutableArray<string> parameterNames)
        {
            var parameterList = method.Parameters;
            if (parameterList.Select(p => p.Name).SequenceEqual(parameterNames))
            {
                return method;
            }

            var parameters = parameterList.RenameParameters(parameterNames);

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                method.ContainingType,
                method.GetAttributes(),
                method.DeclaredAccessibility,
                method.GetSymbolModifiers(),
                method.ReturnType,
                method.RefKind,
                method.ExplicitInterfaceImplementations,
                method.Name,
                method.TypeParameters,
                parameters);
        }

        private static ImmutableArray<ITypeParameterSymbol> RenameTypeParameters(
            ImmutableArray<ITypeParameterSymbol> typeParameters,
            ImmutableArray<string> newNames,
            ITypeGenerator typeGenerator)
        {
            // We generate the type parameter in two passes.  The first creates the new type
            // parameter.  The second updates the constraints to point at this new type parameter.
            var newTypeParameters = new List<CodeGenerationTypeParameterSymbol>();

            var mapping = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            for (var i = 0; i < typeParameters.Length; i++)
            {
                var typeParameter = typeParameters[i];

                var newTypeParameter = new CodeGenerationTypeParameterSymbol(
                    typeParameter.ContainingType,
                    typeParameter.GetAttributes(),
                    typeParameter.Variance,
                    newNames[i],
                    typeParameter.NullableAnnotation,
                    typeParameter.ConstraintTypes,
                    typeParameter.HasConstructorConstraint,
                    typeParameter.HasReferenceTypeConstraint,
                    typeParameter.HasValueTypeConstraint,
                    typeParameter.HasUnmanagedTypeConstraint,
                    typeParameter.HasNotNullConstraint,
                    typeParameter.Ordinal);

                newTypeParameters.Add(newTypeParameter);
                mapping[typeParameter] = newTypeParameter;
            }

            // Now we update the constraints.
            foreach (var newTypeParameter in newTypeParameters)
            {
                newTypeParameter.ConstraintTypes = ImmutableArray.CreateRange(newTypeParameter.ConstraintTypes, t => t.SubstituteTypes(mapping, typeGenerator));
            }

            return newTypeParameters.Cast<ITypeParameterSymbol>().ToImmutableArray();
        }

        public static IMethodSymbol EnsureNonConflictingNames(
            this IMethodSymbol method, INamedTypeSymbol containingType, ISyntaxFactsService syntaxFacts)
        {
            // The method's type parameters may conflict with the type parameters in the type
            // we're generating into.  In that case, rename them.
            var parameterNames = NameGenerator.EnsureUniqueness(
                method.Parameters.SelectAsArray(p => p.Name), isCaseSensitive: syntaxFacts.IsCaseSensitive);

            var outerTypeParameterNames =
                containingType.GetAllTypeParameters()
                              .Select(tp => tp.Name)
                              .Concat(method.Name)
                              .Concat(containingType.Name);

            var unusableNames = parameterNames.Concat(outerTypeParameterNames).ToSet(
                syntaxFacts.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

            var newTypeParameterNames = NameGenerator.EnsureUniqueness(
                method.TypeParameters.SelectAsArray(tp => tp.Name),
                n => !unusableNames.Contains(n));

            var updatedMethod = method.RenameTypeParameters(newTypeParameterNames);
            return updatedMethod.RenameParameters(parameterNames);
        }

        public static IMethodSymbol RemoveInaccessibleAttributesAndAttributesOfTypes(
            this IMethodSymbol method, ISymbol accessibleWithin,
            params INamedTypeSymbol[] removeAttributeTypes)
        {
            var methodHasAttribute = method.GetAttributes().Any(shouldRemoveAttribute);

            var someParameterHasAttribute = method.Parameters
                .Any(m => m.GetAttributes().Any(shouldRemoveAttribute));

            var returnTypeHasAttribute = method.GetReturnTypeAttributes().Any(shouldRemoveAttribute);

            if (!methodHasAttribute && !someParameterHasAttribute && !returnTypeHasAttribute)
            {
                return method;
            }

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                method,
                containingType: method.ContainingType,
                explicitInterfaceImplementations: method.ExplicitInterfaceImplementations,
                attributes: method.GetAttributes().WhereAsArray(a => !shouldRemoveAttribute(a)),
                parameters: method.Parameters.SelectAsArray(p =>
                    CodeGenerationSymbolFactory.CreateParameterSymbol(
                        p.GetAttributes().WhereAsArray(a => !shouldRemoveAttribute(a)),
                        p.RefKind, p.IsParams, p.Type, p.Name, p.IsOptional,
                        p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null)),
                returnTypeAttributes: method.GetReturnTypeAttributes().WhereAsArray(a => !shouldRemoveAttribute(a)));

            bool shouldRemoveAttribute(AttributeData a) =>
                removeAttributeTypes.Any(attr => attr.Equals(a.AttributeClass)) ||
                a.AttributeClass?.IsAccessibleWithin(accessibleWithin) == false;
        }

        public static bool? IsMoreSpecificThan(this IMethodSymbol method1, IMethodSymbol method2)
        {
            var p1 = method1.Parameters;
            var p2 = method2.Parameters;

            // If the methods don't have the same parameter count, then method1 can't be more or 
            // less specific than method2.
            if (p1.Length != p2.Length)
            {
                return null;
            }

            // If the methods' parameter types differ, or they have different names, then one can't
            // be more specific than the other.
            if (!SignatureComparer.Instance.HaveSameSignature(method1.Parameters, method2.Parameters) ||
                !method1.Parameters.Select(p => p.Name).SequenceEqual(method2.Parameters.Select(p => p.Name)))
            {
                return null;
            }

            // Ok.  We have two methods that look extremely similar to each other.  However, one might
            // be more specific if, for example, it was actually written with concrete types (like 'int') 
            // versus the other which may have been instantiated from a type parameter.   i.e.
            //
            // class C<T> { void Goo(T t); void Goo(int t); }
            //
            // THe latter Goo is more specific when comparing "C<int>.Goo(int t)" (method1) vs 
            // "C<int>.Goo(int t)" (method2).
            p1 = method1.OriginalDefinition.Parameters;
            p2 = method2.OriginalDefinition.Parameters;
            return p1.Select(p => p.Type).ToList().AreMoreSpecificThan(p2.Select(p => p.Type).ToList());
        }
    }
}
