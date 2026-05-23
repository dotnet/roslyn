// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Razor;

internal class VsEnumBSTR : IVsEnumBSTR
{
    // Internal for testing
    internal readonly IReadOnlyList<string> Values;

    private int _currentIndex;

    public VsEnumBSTR(IReadOnlyList<string> values)
    {
        Values = values;
        _currentIndex = 0;
    }

    public int Clone(out IVsEnumBSTR ppEnum)
    {
        ppEnum = new VsEnumBSTR(Values);
        return VSConstants.S_OK;
    }

    public int GetCount(out uint pceltCount)
    {
        pceltCount = (uint)Values.Count;
        return VSConstants.S_OK;
    }

    public int Next(uint celt, string[] rgelt, out uint pceltFetched)
    {
        var i = 0;
        for (; i < celt && _currentIndex < Values.Count; i++, _currentIndex++)
        {
            rgelt[i] = Values[_currentIndex];
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
        return _currentIndex < Values.Count
            ? VSConstants.S_OK
            : VSConstants.S_FALSE;
    }
}
