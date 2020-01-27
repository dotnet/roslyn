// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Describes anonymous type field in terms of its name, type and other attributes
    /// </summary>
    internal struct AnonymousTypeField
    {
        /// <summary>Anonymous type field name, not nothing and not empty</summary>
        public readonly string Name;

        /// <summary>Anonymous type field location</summary>
        public readonly Location Location;

        /// <summary>Anonymous type field type with annotations</summary>
        public readonly TypeWithAnnotations TypeWithAnnotations;

        /// <summary>Anonymous type field type</summary>
        public TypeSymbol Type => TypeWithAnnotations.Type;

        public AnonymousTypeField(string name, Location location, TypeWithAnnotations typeWithAnnotations)
        {
            this.Name = name;
            this.Location = location;
            this.TypeWithAnnotations = typeWithAnnotations;
        }

        [Conditional("DEBUG")]
        internal void AssertIsGood()
        {
            Debug.Assert(this.Name != null && this.Location != null && this.TypeWithAnnotations.HasType);
        }
    }
}
