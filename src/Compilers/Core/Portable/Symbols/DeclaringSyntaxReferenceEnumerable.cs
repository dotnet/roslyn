// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public readonly struct SyntaxReferenceEnumerable : IEnumerable<SyntaxReference>
    {
        private readonly ISymbol _symbol;
        private readonly Func<ISymbol, int, (int? nextIndex, SyntaxReference syntaxReference)> _moveNext;

        public SyntaxReferenceEnumerable(ISymbol symbol, Func<ISymbol, int, (int? nextIndex, SyntaxReference syntaxReference)> moveNext)
        {
            _symbol = symbol;
            _moveNext = moveNext;
        }

        public static SyntaxReferenceEnumerable Empty =>
            new SyntaxReferenceEnumerable(symbol: null, (symbol, index) => (default(int?), default(SyntaxReference)));

        public SyntaxReferenceEnumerator GetEnumerator()
        {
            return new SyntaxReferenceEnumerator(_symbol, _moveNext);
        }

        IEnumerator<SyntaxReference> IEnumerable<SyntaxReference>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
