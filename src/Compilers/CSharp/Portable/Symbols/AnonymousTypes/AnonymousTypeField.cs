// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Describes anonymous type field in terms of its name, type and other attributes
    /// </summary>
    internal readonly struct AnonymousTypeField
    {
        /// <summary>Anonymous type field name, not nothing and not empty</summary>
        public readonly string Name;

        /// <summary>Anonymous type field location</summary>
        public readonly Location Location;

        /// <summary>Anonymous type field type with annotations</summary>
        public readonly TypeWithAnnotations TypeWithAnnotations;

        public readonly RefKind RefKind;

        public readonly ScopedKind Scope;

        public readonly ConstantValue? DefaultValue;

        public readonly bool IsParams;

        public readonly bool HasUnscopedRefAttribute;

        /// <summary>Anonymous type field type</summary>
        public TypeSymbol Type => TypeWithAnnotations.Type;

        public AnonymousTypeField(
            string name,
            Location location,
            TypeWithAnnotations typeWithAnnotations,
            RefKind refKind,
            ScopedKind scope,
            ConstantValue? defaultValue = null,
            bool isParams = false,
            bool hasUnscopedRefAttribute = false)
        {
            Debug.Assert(!isParams || !typeWithAnnotations.Type.IsTypeParameter());
            this.Name = name;
            this.Location = location;
            this.TypeWithAnnotations = typeWithAnnotations;
            this.RefKind = refKind;
            this.Scope = scope;
            this.DefaultValue = defaultValue;
            this.IsParams = isParams;
            this.HasUnscopedRefAttribute = hasUnscopedRefAttribute;
        }

        public AnonymousTypeField WithType(TypeWithAnnotations type)
        {
            return new AnonymousTypeField(Name, Location, type, RefKind, Scope, DefaultValue, IsParams, HasUnscopedRefAttribute);
        }

        internal static bool Equals(in AnonymousTypeField x, in AnonymousTypeField y, TypeCompareKind comparison)
        {
            return x.TypeWithAnnotations.Equals(y.TypeWithAnnotations, comparison)
                && x.RefKind == y.RefKind
                && x.Scope == y.Scope
                && x.DefaultValue == y.DefaultValue
                && x.IsParams == y.IsParams
                && x.HasUnscopedRefAttribute == y.HasUnscopedRefAttribute;
        }

        [Conditional("DEBUG")]
        internal void AssertIsGood()
        {
            Debug.Assert(this.Name != null && this.Location != null && this.TypeWithAnnotations.HasType);
        }
    }
}
