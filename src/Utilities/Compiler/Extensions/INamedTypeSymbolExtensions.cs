// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class INamedTypeSymbolExtensions
    {
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
        /// If <paramref name="parentType"/> is the class <code>Stack&gt;T></code>, then this
        /// method will return <code>true</code> when called on <code>Stack&gt;int></code>
        /// or any type derived it, because <code>Stack&gt;int></code> is constructed from
        /// <code>Stack&gt;T></code>.
        /// </para>
        /// <para>
        /// Similarly, if <paramref name="parentType"/> is the interface <code>IList&gt;T></code>, 
        /// then this method will return <code>true</code> for <code>List&gt;int></code>
        /// or any other class that extends <code>IList&gt;></code> or an class that implements it,
        /// because <code>IList&gt;int></code> is constructed from <code>IList&gt;T></code>.
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

        /// <summary>
        /// Returns a value indicating whether the specified type implements the comparison
        /// operators.
        /// </summary>
        /// <param name="symbol">
        /// A symbols specifying the type to examine.
        /// </param>
        /// <returns>
        /// true if the type specified by <paramref name="symbol"/> implements the comparison
        /// operators (which includes the equality and inequality operators), otherwise false.
        /// </returns>
        public static bool ImplementsComparisonOperators(this INamedTypeSymbol symbol)
        {
            return symbol.ImplementsEqualityOperators() &&
                   symbol.ImplementsOperator(WellKnownMemberNames.LessThanOperatorName) &&
                   symbol.ImplementsOperator(WellKnownMemberNames.LessThanOrEqualOperatorName) &&
                   symbol.ImplementsOperator(WellKnownMemberNames.GreaterThanOperatorName) &&
                   symbol.ImplementsOperator(WellKnownMemberNames.GreaterThanOrEqualOperatorName);
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
        /// <c>true</c> if <paramref name="symbol"/> is a static holder type;
        /// otherwise <c>false</c>.
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

            // If the class inherits from another object, or implements some interface, presumably the user meant for the class to be instanciated. This
            // will also bail out if the user inherits from an empty interface, typically used as a marker of some kind. We assume that if _any_ interface
            // is inherited, the user meant to instanciate the type.
            if (symbol.BaseType == null || symbol.BaseType.SpecialType != SpecialType.System_Object || !symbol.AllInterfaces.IsDefaultOrEmpty)
            {
                return false;
            }

            // Sealed objects are presumed to be non-static holder types
            if (symbol.IsSealed)
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
        /// <c>true</c> if <paramref name="member"/> qualifies as a member of
        /// a static holder class; otherwise <c>false</c>.
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
        /// <c>true</c> if the presence of <paramref name="member"/> disqualifies the
        /// current type as a static holder class; otherwise <c>false</c>.
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
    }
}
