﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class INamedTypeSymbolExtensions
    {

        private static readonly Func<INamedTypeSymbol, bool> s_isFileLocal = LightupHelpers.CreateSymbolPropertyAccessor<INamedTypeSymbol, bool>(typeof(INamedTypeSymbol), nameof(IsFileLocal), fallbackResult: false);

        public static bool IsFileLocal(this INamedTypeSymbol symbol) => s_isFileLocal(symbol);

        public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol type)
        {
            INamedTypeSymbol current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        /// <summary>
        /// Returns a value indicating whether <paramref name="type"/> derives from, or implements
        /// any generic construction of, the type defined by <paramref name="parentType"/>.
        /// </summary>
        /// <remarks>
        /// This method only works when <paramref name="parentType"/> is a definition,
        /// not a constructed type.
        /// </remarks>
        /// <example>
        /// <para>
        /// If <paramref name="parentType"/> is the class <see cref="Stack{T}"/>, then this
        /// method will return <see langword="true"/> when called on <c>Stack&gt;int></c>
        /// or any type derived it, because <c>Stack&gt;int></c> is constructed from
        /// <see cref="Stack{T}"/>.
        /// </para>
        /// <para>
        /// Similarly, if <paramref name="parentType"/> is the interface <see cref="IList{T}"/>,
        /// then this method will return <see langword="true"/> for <c>List&gt;int></c>
        /// or any other class that extends <see cref="IList{T}"/> or an class that implements it,
        /// because <c>IList&gt;int></c> is constructed from <see cref="IList{T}"/>.
        /// </para>
        /// </example>
        public static bool DerivesFromOrImplementsAnyConstructionOf(this INamedTypeSymbol type, INamedTypeSymbol parentType)
        {
            if (!parentType.IsDefinition)
            {
                throw new ArgumentException($"The type {nameof(parentType)} is not a definition; it is a constructed type", nameof(parentType));
            }

            for (INamedTypeSymbol? baseType = type.OriginalDefinition;
                baseType != null;
                baseType = baseType.BaseType?.OriginalDefinition)
            {
                if (baseType.Equals(parentType))
                {
                    return true;
                }
            }

            if (type.OriginalDefinition.AllInterfaces.Any(baseInterface => baseInterface.OriginalDefinition.Equals(parentType)))
            {
                return true;
            }

            return false;
        }

        public static bool ImplementsOperator(this INamedTypeSymbol symbol, string op)
        {
            // TODO: should this filter on the right-hand-side operator type?
            return symbol.GetMembers(op).OfType<IMethodSymbol>().Any(m => m.MethodKind == MethodKind.UserDefinedOperator);
        }

        /// <summary>
        /// Returns a value indicating whether the specified type implements both the
        /// equality and inequality operators.
        /// </summary>
        /// <param name="symbol">
        /// A symbols specifying the type to examine.
        /// </param>
        /// <returns>
        /// true if the type specified by <paramref name="symbol"/> implements both the
        /// equality and inequality operators, otherwise false.
        /// </returns>
        public static bool ImplementsEqualityOperators(this INamedTypeSymbol symbol)
        {
            return symbol.ImplementsOperator(WellKnownMemberNames.EqualityOperatorName) &&
                   symbol.ImplementsOperator(WellKnownMemberNames.InequalityOperatorName);
        }

        public static bool OverridesEquals(this INamedTypeSymbol symbol)
        {
            // Does the symbol override Object.Equals?
            return symbol.GetMembers(WellKnownMemberNames.ObjectEquals).OfType<IMethodSymbol>().Any(m => m.IsObjectEqualsOverride());
        }

        public static bool OverridesGetHashCode(this INamedTypeSymbol symbol)
        {
            // Does the symbol override Object.GetHashCode?
            return symbol.GetMembers(WellKnownMemberNames.ObjectGetHashCode).OfType<IMethodSymbol>().Any(m => m.IsGetHashCodeOverride());
        }

        public static bool HasFinalizer(this INamedTypeSymbol symbol)
        {
            return symbol.GetMembers()
                .Any(m => m is IMethodSymbol method && method.IsFinalizer());
        }

        /// <summary>
        /// Returns a value indicating whether the specified symbol is a static
        /// holder type.
        /// </summary>
        /// <param name="symbol">
        /// The symbol being examined.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="symbol"/> is a static holder type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// A symbol is a static holder type if it is a class with at least one
        /// "qualifying member" (<see cref="IsQualifyingMember(ISymbol)"/>) and no
        /// "disqualifying members" (<see cref="IsDisqualifyingMember(ISymbol)"/>).
        /// </remarks>
        public static bool IsStaticHolderType(this INamedTypeSymbol symbol)
        {
            if (symbol.TypeKind != TypeKind.Class)
            {
                return false;
            }

            // If the class inherits from another object, or implements some interface, presumably the user meant for the class to be instantiated. This
            // will also bail out if the user inherits from an empty interface, typically used as a marker of some kind. We assume that if _any_ interface
            // is inherited, the user meant to instantiate the type.
            if (symbol.BaseType == null || symbol.BaseType.SpecialType != SpecialType.System_Object || !symbol.AllInterfaces.IsDefaultOrEmpty)
            {
                return false;
            }

            // Sealed objects are presumed to be non-static holder types for C#.
            // In VB.NET the type cannot be static and guidelines favor having a sealed (NotInheritable) type
            //  to act as static holder type.
            if (symbol.IsSealed && symbol.Language == LanguageNames.CSharp)
            {
                return false;
            }

            // Same as
            // return declaredMembers.Any(IsQualifyingMember) && !declaredMembers.Any(IsDisqualifyingMember);
            // but with less enumerations
            var hasQualifyingMembers = false;
            foreach (var member in symbol.GetMembers())
            {
                if (!member.IsImplicitlyDeclared)
                {
                    if (!hasQualifyingMembers && IsQualifyingMember(member))
                    {
                        hasQualifyingMembers = true;
                    }

                    if (IsDisqualifyingMember(member))
                    {
                        return false;
                    }
                }
            }

            return hasQualifyingMembers;
        }

        /// <summary>
        /// Returns a value indicating whether the specified symbol qualifies as a
        /// member of a static holder class.
        /// </summary>
        /// <param name="member">
        /// The member being examined.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="member"/> qualifies as a member of
        /// a static holder class; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsQualifyingMember(ISymbol member)
        {
            // A type member *does* qualify as a member of a static holder class,
            // because even though it is *not* static, it is nevertheless not
            // per-instance.
            if (member.IsType())
            {
                return true;
            }

            // An user-defined operator method is not a valid member of a static holder
            // class, because even though it is static, it takes instances as
            // parameters, so presumably the author of the class intended for it to be
            // instantiated.
            if (member.IsUserDefinedOperator())
            {
                return false;
            }

            // A static constructor does not qualify or disqualify a class from being a
            // static holder, because it isn't accessible to any consumers of the class.
            if (member.IsConstructor())
            {
                return false;
            }

            // Private or protected members do not qualify or disqualify a class from
            // being a static holder class, because they are not accessible to any
            // consumers of the class.
            if (member.IsProtected() || member.IsPrivate())
            {
                return false;
            }

            return member.IsStatic;
        }

        /// <summary>
        /// Returns a value indicating whether the presence of the specified symbol
        /// disqualifies a class from being considered a static holder class.
        /// </summary>
        /// <param name="member">
        /// The member being examined.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the presence of <paramref name="member"/> disqualifies the
        /// current type as a static holder class; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDisqualifyingMember(ISymbol member)
        {
            // An user-defined operator method disqualifies a class from being considered
            // a static holder, because even though it is static, it takes instances as
            // parameters, so presumably the author of the class intended for it to be
            // instantiated.
            if (member.IsUserDefinedOperator())
            {
                return true;
            }

            // Like user-defined operators, conversion operators disqualify a class
            // from being considered a static holder, because it converts from an instance of
            // another class to this class, so presumably the author intended for it to be
            // instantiated
            if (member.IsConversionOperator())
            {
                return true;
            }

            // A type member does *not* disqualify a class from being considered a static
            // holder, because even though it is *not* static, it is nevertheless not
            // per-instance.
            if (member.IsType())
            {
                return false;
            }

            // Any instance member other than a default constructor disqualifies a class
            // from being considered a static holder class.
            return !member.IsStatic && !member.IsDefaultConstructor();
        }

        public static bool IsBenchmarkOrXUnitTestAttribute(this INamedTypeSymbol attributeClass, ConcurrentDictionary<INamedTypeSymbol, bool> knownTestAttributes, INamedTypeSymbol? benchmarkAttribute, INamedTypeSymbol? xunitFactAttribute)
        {
            if (knownTestAttributes.TryGetValue(attributeClass, out var isTest))
                return isTest;

            var derivedFromKnown =
                (xunitFactAttribute is not null && attributeClass.DerivesFrom(xunitFactAttribute))
                || (benchmarkAttribute is not null && attributeClass.DerivesFrom(benchmarkAttribute));
            return knownTestAttributes.GetOrAdd(attributeClass, derivedFromKnown);
        }

        /// <summary>
        /// Check if the given <paramref name="typeSymbol"/> is an implicitly generated type for top level statements.
        /// </summary>
        public static bool IsTopLevelStatementsEntryPointType([NotNullWhen(true)] this INamedTypeSymbol? typeSymbol)
            => typeSymbol is not null &&
               typeSymbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.IsTopLevelStatementsEntryPointMethod());
    }
}
