// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class VsEnumDebugName : IVsEnumDebugName
    {
        private readonly IList<IVsDebugName> _values;
        private int _currentIndex;

        public VsEnumDebugName(IList<IVsDebugName> values)
        {
            _values = values;
            _currentIndex = 0;
        }

        public int Clone(out IVsEnumDebugName ppEnum)
        {
            ppEnum = new VsEnumDebugName(_values);
            return VSConstants.S_OK;
        }

        public int GetCount(out uint pceltCount)
        {
            pceltCount = (uint)_values.Count;
            return VSConstants.S_OK;
        }

        public int Next(uint celt, IVsDebugName[] rgelt, uint[] pceltFetched)
        {
            var i = 0;
            for (; i < celt && _currentIndex < _values.Count; i++, _currentIndex++)
            {
                rgelt[i] = _values[_currentIndex];
            }

            if (pceltFetched != null && pceltFetched.Length > 0)
            {
                pceltFetched[0] = (uint)i;
            }

            return i < celt
                ? VSConstants.S_FALSE
                : VSConstants.S_OK;
        }

        public int Reset()
        {
            _currentIndex = 0;
            return VSConstants.S_OK;
        }

        public int Skip(uint celt)
        {
            _currentIndex += (int)celt;
            return _currentIndex < _values.Count
                ? VSConstants.S_OK
                : VSConstants.S_FALSE;
        }
    }
}
