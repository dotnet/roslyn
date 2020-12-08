// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Implemented by symbols that can be targeted by an attribute declaration (i.e. source symbols).
    /// </summary>
    internal interface IAttributeTargetSymbol
    {
        /// <summary>
        /// Returns the owner of attributes that apply to this symbol.
        /// </summary>
        /// <remarks>
        /// Attributes for this symbol might be retrieved from attribute list of another (owning) symbol.
        /// In that case this property returns that owning symbol, otherwise it returns "this".
        /// </remarks>
        IAttributeTargetSymbol AttributesOwner { get; }

        /// <summary>
        /// Returns a bit set of attribute locations applicable to this symbol.
        /// </summary>
        AttributeLocation AllowedAttributeLocations { get; }

        /// <summary>
        /// Attribute location corresponding to this symbol.
        /// </summary>
        /// <remarks>
        /// Location of an attribute if an explicit location is not specified via attribute target specification syntax.
        /// </remarks>
        AttributeLocation DefaultAttributeLocation { get; }

        // TODO (tomat): 
        // Add DecodeWellKnownAttribute, etc.
    }
}
