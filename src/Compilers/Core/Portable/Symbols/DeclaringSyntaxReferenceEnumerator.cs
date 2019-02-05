// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public struct SyntaxReferenceEnumerator : IEnumerator<SyntaxReference>
    {
        private readonly ISymbol _symbol;
        private readonly Func<ISymbol, int, (int? nextIndex, SyntaxReference syntaxReference)> _moveNext;
        private int _index;
        private SyntaxReference _current;

        public SyntaxReferenceEnumerator(ISymbol symbol, Func<ISymbol, int, (int? nextIndex, SyntaxReference syntaxReference)> moveNext)
        {
            _symbol = symbol;
            _moveNext = moveNext;
            _index = -1;
            _current = null;
        }

        public SyntaxReference Current => _current;
        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            var (nextIndex, syntaxReference) = _moveNext(_symbol, _index);
            if (nextIndex == null)
            {
                _current = null;
                return false;
            }

            _index = nextIndex.Value;
            _current = syntaxReference;
            return true;
        }

        public void Reset()
        {
            _index = -1;
            _current = null;
        }
    }
}
