﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IMethodSymbolExtensions
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

            for (int i = 0; i < method.Parameters.Length; i++)
            {
                if (!invoke.Parameters[i].Type.InheritsFromOrEquals(method.Parameters[i].Type))
                {
                    return false;
                }
            }

            return true;
        }

        public static IMethodSymbol RenameTypeParameters(this IMethodSymbol method, IList<string> newNames)
        {
            if (method.TypeParameters.Select(t => t.Name).SequenceEqual(newNames))
            {
                return method;
            }

            var typeGenerator = new TypeGenerator();
            var updatedTypeParameters = RenameTypeParameters(
                method.TypeParameters, newNames, typeGenerator);

            var mapping = new Dictionary<ITypeSymbol, ITypeSymbol>();
            for (int i = 0; i < method.TypeParameters.Length; i++)
            {
                mapping.Add(method.TypeParameters[i], updatedTypeParameters[i]);
            }

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                method.ContainingType,
                method.GetAttributes(),
                method.DeclaredAccessibility,
                method.GetSymbolModifiers(),
                method.ReturnType.SubstituteTypes(mapping, typeGenerator),
                method.ExplicitInterfaceImplementations.FirstOrDefault(),
                method.Name,
                updatedTypeParameters,
                method.Parameters.Select(p =>
                    CodeGenerationSymbolFactory.CreateParameterSymbol(p.GetAttributes(), p.RefKind, p.IsParams, p.Type.SubstituteTypes(mapping, typeGenerator), p.Name, p.IsOptional,
                        p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null)).ToList());
        }

        public static IMethodSymbol RenameParameters(this IMethodSymbol method, IList<string> parameterNames)
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
                method.ExplicitInterfaceImplementations.FirstOrDefault(),
                method.Name,
                method.TypeParameters,
                parameters);
        }

        private static IList<ITypeParameterSymbol> RenameTypeParameters(
            IList<ITypeParameterSymbol> typeParameters,
            IList<string> newNames,
            ITypeGenerator typeGenerator)
        {
            // We generate the type parameter in two passes.  The first creates the new type
            // parameter.  The second updates the constraints to point at this new type parameter.
            var newTypeParameters = new List<CodeGenerationTypeParameterSymbol>();
            var mapping = new Dictionary<ITypeSymbol, ITypeSymbol>();
            for (int i = 0; i < typeParameters.Count; i++)
            {
                var typeParameter = typeParameters[i];

                var newTypeParameter = new CodeGenerationTypeParameterSymbol(
                    typeParameter.ContainingType,
                    typeParameter.GetAttributes(),
                    typeParameter.Variance,
                    newNames[i],
                    typeParameter.ConstraintTypes,
                    typeParameter.HasConstructorConstraint,
                    typeParameter.HasReferenceTypeConstraint,
                    typeParameter.HasValueTypeConstraint,
                    typeParameter.Ordinal);

                newTypeParameters.Add(newTypeParameter);
                mapping.Add(typeParameter, newTypeParameter);
            }

            // Now we update the constraints.
            foreach (var newTypeParameter in newTypeParameters)
            {
                newTypeParameter.ConstraintTypes = ImmutableArray.CreateRange(newTypeParameter.ConstraintTypes, t => t.SubstituteTypes(mapping, typeGenerator));
            }

            return newTypeParameters.Cast<ITypeParameterSymbol>().ToList();
        }

        public static IMethodSymbol EnsureNonConflictingNames(
            this IMethodSymbol method, INamedTypeSymbol containingType, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            // The method's type parameters may conflict with the type parameters in the type
            // we're generating into.  In that case, rename them.
            var parameterNames = NameGenerator.EnsureUniqueness(
                method.Parameters.Select(p => p.Name).ToList(), isCaseSensitive: syntaxFacts.IsCaseSensitive);

            var outerTypeParameterNames =
                containingType.GetAllTypeParameters()
                              .Select(tp => tp.Name)
                              .Concat(method.Name)
                              .Concat(containingType.Name);

            var unusableNames = parameterNames.Concat(outerTypeParameterNames).ToSet(
                syntaxFacts.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

            var newTypeParameterNames = NameGenerator.EnsureUniqueness(
                method.TypeParameters.Select(tp => tp.Name).ToList(),
                n => !unusableNames.Contains(n));

            var updatedMethod = method.RenameTypeParameters(newTypeParameterNames);
            return updatedMethod.RenameParameters(parameterNames);
        }

        public static IMethodSymbol RemoveInaccessibleAttributesAndAttributesOfType(
            this IMethodSymbol method, ISymbol accessibleWithin, INamedTypeSymbol removeAttributeType,
            IList<SyntaxNode> statements = null, IList<SyntaxNode> handlesExpressions = null)
        {
            Func<AttributeData, bool> shouldRemoveAttribute = a =>
                a.AttributeClass.Equals(removeAttributeType) || !a.AttributeClass.IsAccessibleWithin(accessibleWithin);
            return method.RemoveAttributesCore(shouldRemoveAttribute, statements, handlesExpressions);
        }

        private static IMethodSymbol RemoveAttributesCore(
            this IMethodSymbol method, Func<AttributeData, bool> shouldRemoveAttribute,
            IList<SyntaxNode> statements, IList<SyntaxNode> handlesExpressions)
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
                method.ContainingType,
                method.GetAttributes().Where(a => !shouldRemoveAttribute(a)).ToList(),
                method.DeclaredAccessibility,
                method.GetSymbolModifiers(),
                method.ReturnType,
                method.ExplicitInterfaceImplementations.FirstOrDefault(),
                method.Name,
                method.TypeParameters,
                method.Parameters.Select(p =>
                    CodeGenerationSymbolFactory.CreateParameterSymbol(
                        p.GetAttributes().Where(a => !shouldRemoveAttribute(a)).ToList(),
                        p.RefKind, p.IsParams, p.Type, p.Name, p.IsOptional,
                        p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null)).ToList(),
                statements,
                handlesExpressions,
                method.GetReturnTypeAttributes().Where(a => !shouldRemoveAttribute(a)).ToList());
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
            // class C<T> { void Foo(T t); void Foo(int t); }
            //
            // THe latter Foo is more specific when comparing "C<int>.Foo(int t)" (method1) vs 
            // "C<int>.Foo(int t)" (method2).
            p1 = method1.OriginalDefinition.Parameters;
            p2 = method2.OriginalDefinition.Parameters;
            return p1.Select(p => p.Type).ToList().AreMoreSpecificThan(p2.Select(p => p.Type).ToList());
        }

        public static bool TryGetPredefinedComparisonOperator(this IMethodSymbol symbol, out PredefinedOperator op)
        {
            if (symbol.MethodKind == MethodKind.BuiltinOperator)
            {
                op = symbol.GetPredefinedOperator();
                switch (op)
                {
                    case PredefinedOperator.Equality:
                    case PredefinedOperator.Inequality:
                    case PredefinedOperator.GreaterThanOrEqual:
                    case PredefinedOperator.LessThanOrEqual:
                    case PredefinedOperator.GreaterThan:
                    case PredefinedOperator.LessThan:
                        return true;
                }
            }
            else
            {
                op = PredefinedOperator.None;
            }

            return false;
        }

        public static PredefinedOperator GetPredefinedOperator(this IMethodSymbol symbol)
        {
            switch (symbol.Name)
            {
                case "op_Addition":
                case "op_UnaryPlus":
                    return PredefinedOperator.Addition;
                case "op_BitwiseAnd":
                    return PredefinedOperator.BitwiseAnd;
                case "op_BitwiseOr":
                    return PredefinedOperator.BitwiseOr;
                case "op_Concatenate":
                    return PredefinedOperator.Concatenate;
                case "op_Decrement":
                    return PredefinedOperator.Decrement;
                case "op_Division":
                    return PredefinedOperator.Division;
                case "op_Equality":
                    return PredefinedOperator.Equality;
                case "op_ExclusiveOr":
                    return PredefinedOperator.ExclusiveOr;
                case "op_Exponent":
                    return PredefinedOperator.Exponent;
                case "op_GreaterThan":
                    return PredefinedOperator.GreaterThan;
                case "op_GreaterThanOrEqual":
                    return PredefinedOperator.GreaterThanOrEqual;
                case "op_Increment":
                    return PredefinedOperator.Increment;
                case "op_Inequality":
                    return PredefinedOperator.Inequality;
                case "op_IntegerDivision":
                    return PredefinedOperator.IntegerDivision;
                case "op_LeftShift":
                    return PredefinedOperator.LeftShift;
                case "op_LessThan":
                    return PredefinedOperator.LessThan;
                case "op_LessThanOrEqual":
                    return PredefinedOperator.LessThanOrEqual;
                case "op_Like":
                    return PredefinedOperator.Like;
                case "op_LogicalNot":
                case "op_OnesComplement":
                    return PredefinedOperator.Complement;
                case "op_Modulus":
                    return PredefinedOperator.Modulus;
                case "op_Multiply":
                    return PredefinedOperator.Multiplication;
                case "op_RightShift":
                    return PredefinedOperator.RightShift;
                case "op_Subtraction":
                case "op_UnaryNegation":
                    return PredefinedOperator.Subtraction;
                default:
                    return PredefinedOperator.None;
            }
        }
    }
}
