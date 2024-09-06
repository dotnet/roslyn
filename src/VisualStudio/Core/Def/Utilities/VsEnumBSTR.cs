// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

internal class VsEnumBSTR : IVsEnumBSTR
{
    private readonly IList<string> _values;
    private int _currentIndex;

    public VsEnumBSTR(IList<string> values)
    {
        _values = values;
        _currentIndex = 0;
    }

    public int Clone(out IVsEnumBSTR ppEnum)
    {
        ppEnum = new VsEnumBSTR(_values);
        return VSConstants.S_OK;
    }

    public int GetCount(out uint pceltCount)
    {
        pceltCount = (uint)_values.Count;
        return VSConstants.S_OK;
    }

    public int Next(uint celt, string[] rgelt, out uint pceltFetched)
    {
        var i = 0;
        for (; i < celt && _currentIndex < _values.Count; i++, _currentIndex++)
        {
            rgelt[i] = _values[_currentIndex];
        }

        pceltFetched = (uint)i;
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
