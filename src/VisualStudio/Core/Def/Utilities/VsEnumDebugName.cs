// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

internal sealed class VsEnumDebugName : IVsEnumDebugName
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
