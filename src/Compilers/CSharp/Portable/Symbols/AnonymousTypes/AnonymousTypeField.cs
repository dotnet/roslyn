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

        public readonly DeclarationScope Scope;

        public readonly ConstantValue? DefaultValue;

        public readonly SourceComplexParameterSymbolBase? ParameterSymbolForAttributes;

        /// <summary>Anonymous type field type</summary>
        public TypeSymbol Type => TypeWithAnnotations.Type;

        // PROTOTYPE: Sync with IDE about the addition of DefaultValue to AnonymousTypeField to see how it will affect their usage of anonymous type symbols
        public AnonymousTypeField(string name, Location location, TypeWithAnnotations typeWithAnnotations, RefKind refKind, DeclarationScope scope, ConstantValue? defaultValue = null, SourceComplexParameterSymbolBase? parameterSymbolForAttributes = null)
        {
            this.Name = name;
            this.Location = location;
            this.TypeWithAnnotations = typeWithAnnotations;
            this.RefKind = refKind;
            this.Scope = scope;
            this.DefaultValue = defaultValue;
            this.ParameterSymbolForAttributes = parameterSymbolForAttributes;
        }

        [Conditional("DEBUG")]
        internal void AssertIsGood()
        {
            Debug.Assert(this.Name != null && this.Location != null && this.TypeWithAnnotations.HasType);
        }
    }
}
