// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo;

internal sealed class NavInfoNode : IVsNavInfoNode
{
    public string Name { get; }
    public _LIB_LISTTYPE ListType { get; }

    public NavInfoNode(string name, uint listType)
    {
        Name = name;
        ListType = (_LIB_LISTTYPE)listType;
    }

    public NavInfoNode(string name, _LIB_LISTTYPE listType)
    {
        Name = name;
        ListType = listType;
    }

    int IVsNavInfoNode.get_Name(out string pbstrName)
    {
        pbstrName = Name;
        return VSConstants.S_OK;
    }

    int IVsNavInfoNode.get_Type(out uint pllt)
    {
        pllt = (uint)ListType;
        return VSConstants.S_OK;
    }
}
