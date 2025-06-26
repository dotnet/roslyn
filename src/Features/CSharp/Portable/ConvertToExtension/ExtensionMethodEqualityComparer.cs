// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToExtension;

internal sealed partial class ConvertToExtensionCodeRefactoringProvider
{
    private sealed class ExtensionMethodEqualityComparer :
        IEqualityComparer<AttributeData>,
        IEqualityComparer<ITypeParameterSymbol>,
        IEqualityComparer<ExtensionMethodInfo>
    {
        public static readonly ExtensionMethodEqualityComparer Instance = new();

        private static readonly SymbolEquivalenceComparer s_equivalenceComparer = new(
            assemblyComparer: null,
            distinguishRefFromOut: true,
            // `void Goo(this (int x, int y) tuple)` doesn't match `void Goo(this (int a, int b) tuple)
            tupleNamesMustMatch: true,
            // `void Goo(this string? x)` doesn't matches `void Goo(this string x)`
            ignoreNullableAnnotations: false,
            // `void Goo(this object x)` doesn't matches `void Goo(this dynamic x)`
            objectAndDynamicCompareEqually: false,
            // `void Goo(this string[] x)` doesn't matches `void Goo(this Span<string> x)`
            arrayAndReadOnlySpanCompareEqually: false);

        #region IEqualityComparer<AttributeData>

        private bool AttributesMatch(ImmutableArray<AttributeData> attributes1, ImmutableArray<AttributeData> attributes2)
            => attributes1.SequenceEqual(attributes2, this);

        public bool Equals(AttributeData? x, AttributeData? y)
        {
            if (x == y)
                return true;

            if (x is null || y is null)
                return false;

            // Ensure the attributes reference the same attribute class, and have the same constructor/named-parameter
            // values in the same order.
            if (!Equals(x.AttributeClass, y.AttributeClass))
                return false;

            return x.ConstructorArguments.SequenceEqual(y.ConstructorArguments) &&
                   x.NamedArguments.SequenceEqual(y.NamedArguments);
        }

        // Not needed as we never group by attributes.  We only SequenceEqual compare them.
        public int GetHashCode([DisallowNull] AttributeData obj)
            => throw ExceptionUtilities.Unreachable();

        #endregion

        #region IEqualityComparer<ITypeParameterSymbol>

        public bool Equals(ITypeParameterSymbol? x, ITypeParameterSymbol? y)
        {
            if (x == y)
                return true;

            if (x is null || y is null)
                return false;

            // Names must match as the code in the extension methods may reference the type parameters by name and has
            // to continue working.
            if (x.Name != y.Name)
                return false;

            // Attributes have to match as we're moving these type parameters up to the extension itself.
            if (!AttributesMatch(x.GetAttributes(), y.GetAttributes()))
                return false;

            // Constraints have to match as we're moving these type parameters up to the extension itself.
            if (x.HasConstructorConstraint != y.HasConstructorConstraint)
                return false;

            if (x.HasNotNullConstraint != y.HasNotNullConstraint)
                return false;

            if (x.HasReferenceTypeConstraint != y.HasReferenceTypeConstraint)
                return false;

            if (x.HasUnmanagedTypeConstraint != y.HasUnmanagedTypeConstraint)
                return false;

            if (x.HasValueTypeConstraint != y.HasValueTypeConstraint)
                return false;

            // Constraints have to match as we're moving these type parameters up to the extension itself. We again use
            // s_equivalenceComparer.SignatureTypeEquivalenceComparer here as we want method type parameters compared by
            // ordinal so that if we constraints that reference the method type parameters, that we can tell they're
            // equivalent across disparate methods.
            if (!x.ConstraintTypes.SequenceEqual(y.ConstraintTypes, s_equivalenceComparer.SignatureTypeEquivalenceComparer))
                return false;

            return true;
        }

        // Not needed as we never group by type parameters.  We only SequenceEqual compare them.
        public int GetHashCode([DisallowNull] ITypeParameterSymbol obj)
            => throw ExceptionUtilities.Unreachable();

        #endregion

        #region IEqualityComparer<ExtensionMethodInfo>

        public bool Equals(ExtensionMethodInfo x, ExtensionMethodInfo y)
        {
            if (x.ExtensionMethod == y.ExtensionMethod)
                return true;

            // For us to consider two extension methods to be equivalent, they must have a first parameter that we
            // consider equal, any method type parameters they use must have the same constraints, and they must have
            // the same attributes on them.
            //
            // Notes: s_equivalenceComparer.ParameterEquivalenceComparer will check the parameter name, type, ref kinds,
            //  custom modifiers.  All things we want to match to merge extension methods into the same method.
            //
            // Note: The initial check will ensure that the same method-type-parameters are used in both methods *when
            // compared by type parameter ordinal*.  The MethodTypeParameterMatch will then check that the type
            // parameters that we would lift to the extension method would be considered the same as well.

            return
                s_equivalenceComparer.ParameterEquivalenceComparer.Equals(x.FirstParameter, y.FirstParameter, compareParameterName: true, isCaseSensitive: true) &&
                AttributesMatch(x.FirstParameter.GetAttributes(), y.FirstParameter.GetAttributes()) &&
                x.MethodTypeParameters.SequenceEqual(y.MethodTypeParameters, this);
        }

        public int GetHashCode(ExtensionMethodInfo obj)
            // Loosely match any extension methods if they have the same first parameter type (treating method type
            // parameters by ordinal) and same name.  We'll do a more full match in .Equals above.
            => s_equivalenceComparer.ParameterEquivalenceComparer.GetHashCode(obj.FirstParameter) ^ obj.FirstParameter.Name.GetHashCode();

        #endregion
    }
}
