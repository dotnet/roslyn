// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that can bind a possible field identifier in a given property accessor for speculative semantic model scenarios.
    /// </summary>
    internal class SpeculativeFieldKeywordBinder : Binder
    {
        private readonly SourcePropertyAccessorSymbol _accessor;

        internal SpeculativeFieldKeywordBinder(SourcePropertyAccessorSymbol accessor, Binder next)
            : base(next)
        {
            _accessor = accessor;
        }

        internal override FieldSymbol? GetSymbolForPossibleFieldKeyword()
        {
            // field in the speculative model does not bind to a backing field if the original location was not a semi-auto property
            return _accessor.Property.FieldKeywordBackingField;
        }
    }
}
