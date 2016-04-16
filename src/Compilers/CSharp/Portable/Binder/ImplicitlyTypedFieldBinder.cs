// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Tracks fields that are being bound while binding their initializers.
    /// </summary>
    /// <remarks>
    /// Used to detect circular references like:
    /// var x = y;
    /// var y = x;
    /// </remarks>
    internal sealed class ImplicitlyTypedFieldBinder : Binder
    {
        private readonly ConsList<FieldSymbol> _fieldsBeingBound;

        public ImplicitlyTypedFieldBinder(Binder next, ConsList<FieldSymbol> fieldsBeingBound)
            : base(next, next.Flags)
        {
            _fieldsBeingBound = fieldsBeingBound;
        }

        internal override ConsList<FieldSymbol> FieldsBeingBound
        {
            get
            {
                return _fieldsBeingBound;
            }
        }
    }
}
