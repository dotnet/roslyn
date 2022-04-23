// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that can bind a possible field identifier in a given property accessor.
    /// </summary>
    internal class FieldKeywordBinder : Binder
    {
        private readonly SourcePropertyAccessorSymbol _accessor;

        internal FieldKeywordBinder(SourcePropertyAccessorSymbol accessor, Binder next)
            : base(next)
        {
            _accessor = accessor;
        }

        internal override FieldSymbol? GetSymbolForPossibleFieldKeyword()
        {
            return _accessor.Property.GetOrCreateBackingFieldForFieldKeyword();
        }
    }
}
