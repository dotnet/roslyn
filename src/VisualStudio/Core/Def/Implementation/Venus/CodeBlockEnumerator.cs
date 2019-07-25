// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    /// <summary>
    /// An implementation of IVsEnumCodeBlocks used in the implementation of
    /// IVsContainedCode.EnumOriginalCodeBlocks for each language's Venus interop layer.
    /// </summary>
    internal class CodeBlockEnumerator : IVsEnumCodeBlocks
    {
        private readonly IList<TextSpanAndCookie> _codeBlocks;
        private int _currentElement;

        public CodeBlockEnumerator(IList<TextSpanAndCookie> codeBlocks)
        {
            _codeBlocks = codeBlocks;
            _currentElement = 0;
        }

        /// <summary>
        /// Clones another instance of a CodeBlockEnumerator.
        /// </summary>
        private CodeBlockEnumerator(CodeBlockEnumerator previousEnumerator)
        {
            _codeBlocks = previousEnumerator._codeBlocks;
            _currentElement = previousEnumerator._currentElement;
        }

        public int Clone(out IVsEnumCodeBlocks ppEnum)
        {
            ppEnum = new CodeBlockEnumerator(this);
            return VSConstants.S_OK;
        }

        public int Next(uint celt, TextSpanAndCookie[] rgelt, out uint pceltFetched)
        {
            pceltFetched = Math.Min(celt, (uint)(_codeBlocks.Count - _currentElement));

            // Copy each element over
            for (var i = 0; i < pceltFetched; i++)
            {
                rgelt[i] = _codeBlocks[_currentElement++];
            }

            // If we returned fewer than the requested number of elements, we return S_FALSE
            return celt == pceltFetched
                ? VSConstants.S_OK
                : VSConstants.S_FALSE;
        }

        public int Reset()
        {
            _currentElement = 0;
            return VSConstants.S_OK;
        }

        public int Skip(uint celt)
        {
            _currentElement += (int)celt;

            // If we've advanced past the end, move back. We return S_FALSE only in this case. If we simply move *to*
            // the end, we actually do want to return S_OK.
            if (_currentElement > _codeBlocks.Count)
            {
                _currentElement = _codeBlocks.Count;
                return VSConstants.S_FALSE;
            }
            else
            {
                return VSConstants.S_OK;
            }
        }
    }
}
