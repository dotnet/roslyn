// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Used for lightweight binding of type constraints. Instead of binding type arguments,
    /// we'll just use these placeholders instead. That's good enough binding to compute
    /// <see cref="TypeSymbol.IsValueType"/> with minimal binding.
    /// </summary>
    internal sealed class PlaceholderTypeArgumentSymbol : ErrorTypeSymbol
    {
        private static readonly TypeWithAnnotations s_instance = TypeWithAnnotations.Create(new PlaceholderTypeArgumentSymbol());

        public static ImmutableArray<TypeWithAnnotations> CreateTypeArguments(ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            return typeParameters.SelectAsArray(_ => s_instance);
        }

        private PlaceholderTypeArgumentSymbol()
        {
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override string Name
        {
            get
            {
                return string.Empty;
            }
        }

        internal override bool MangleName
        {
            get
            {
                Debug.Assert(Arity == 0);
                return false;
            }
        }

        internal override bool IsFileLocal => false;
        internal override FileIdentifier? AssociatedFileIdentifier => null;

        internal override DiagnosticInfo? ErrorInfo
        {
            get
            {
                return null;
            }
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            return (object)t2 == this;
        }

        public override int GetHashCode()
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        }
    }
}

