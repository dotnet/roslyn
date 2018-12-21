﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The effective "bounds" of a type parameter: the constraint types, effective
    /// interface set, and effective base type, determined from the declared
    /// constraints, with any cycles removed. The fields are exposed by the
    /// TypeParameterSymbol as ConstraintTypes, Interfaces, and BaseType.
    /// </summary>
    internal sealed class TypeParameterBounds
    {
        public static readonly TypeParameterBounds Unset = new TypeParameterBounds();

        // https://github.com/dotnet/roslyn/issues/30061: Add static Create methods and have Create
        // return an EarlyEmpty singleton instance for the common case of no constraint types.

        /// <summary>
        /// Creates an "early" bound instance that has constraint types set
        /// but no other fields.
        /// </summary>
        public TypeParameterBounds(ImmutableArray<TypeSymbolWithAnnotations> constraintTypes)
        {
            Debug.Assert(!constraintTypes.IsDefault);
            this.ConstraintTypes = constraintTypes;
        }

        /// <summary>
        /// Creates a "late" bound instance with all fields set.
        /// </summary>
        public TypeParameterBounds(
            ImmutableArray<TypeSymbolWithAnnotations> constraintTypes,
            ImmutableArray<NamedTypeSymbol> interfaces,
            NamedTypeSymbol effectiveBaseClass,
            TypeSymbol deducedBaseType)
        {
            Debug.Assert(!constraintTypes.IsDefault);
            Debug.Assert(!interfaces.IsDefault);
            Debug.Assert((object)effectiveBaseClass != null);
            Debug.Assert((object)deducedBaseType != null);

            this.ConstraintTypes = constraintTypes;
            this.Interfaces = interfaces;
            this.EffectiveBaseClass = effectiveBaseClass;
            this.DeducedBaseType = deducedBaseType;
        }

        private TypeParameterBounds()
        {
        }

        /// <summary>
        /// If true, only ConstraintTypes has been set, as a result of binding syntax.
        /// Bounds have not been calculated, and ConstraintTypes may still
        /// contain invalid types or duplicates.
        /// </summary>
        public bool IsEarly => EffectiveBaseClass is null;

        /// <summary>
        /// The type parameters, classes, and interfaces explicitly declared as
        /// constraint types on the containing type parameter, with cycles removed.
        /// </summary>
        public readonly ImmutableArray<TypeSymbolWithAnnotations> ConstraintTypes;

        /// <summary>
        /// The set of interfaces explicitly declared on the containing type
        /// parameter and any type parameters on which the containing
        /// type parameter depends, with duplicates removed.
        /// </summary>
        public readonly ImmutableArray<NamedTypeSymbol> Interfaces;

        /// <summary>
        /// As defined in 10.1.5 of the specification.
        /// </summary>
        public readonly NamedTypeSymbol EffectiveBaseClass;

        /// <summary>
        /// The "exact" effective base type. 
        /// In the definition of effective base type we abstract some concrete types to their base classes:
        ///  * For each constraint of T that is a struct-type, R contains System.ValueType.
        ///  * For each constraint of T that is an enumeration type, R contains System.Enum.
        ///  * For each constraint of T that is a delegate type, R contains System.Delegate.
        ///  * For each constraint of T that is an array type, R contains System.Array.
        ///  * For each constraint of T that is a class-type C, R contains type C' which is constructed 
        ///    from C by replacing all occurrences of dynamic with object.
        /// The reason is that the CLR doesn't support operations on generic parameters that would be needed 
        /// to work with these types. For example, ldelem instruction requires the receiver to be a specific array, 
        /// not a type parameter constrained to be an array.
        /// 
        /// When computing the deduced type we don't perform this abstraction. We keep the original constraint T.
        /// Deduced base type is used to check that consistency rules are satisfied.
        /// </summary>
        public readonly TypeSymbol DeducedBaseType;
    }

    internal static class TypeParameterBoundsExtensions
    {
        internal static bool IsSet(this TypeParameterBounds boundsOpt, bool early)
        {
            if (boundsOpt == TypeParameterBounds.Unset)
            {
                return false;
            }
            if (boundsOpt == null)
            {
                return true;
            }
            return early || !boundsOpt.IsEarly;
        }
    }
}
