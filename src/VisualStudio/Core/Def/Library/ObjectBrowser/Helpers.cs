// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

internal static class Helpers
{
    public const uint LLF_SEARCH_EXPAND_MEMBERS = 0x0400;
    public const uint LLF_SEARCH_WITH_EXPANSION = 0x0800;

    public const uint LLT_PROJREF = (uint)_LIB_LISTTYPE.LLT_INTERFACEUSEDBYCLASSES;

    public static ObjectListKind ListTypeToObjectListKind(uint listType)
    {
        switch (listType)
        {
            case (uint)_LIB_LISTTYPE.LLT_CLASSES:
                return ObjectListKind.Types;
            case (uint)_LIB_LISTTYPE.LLT_HIERARCHY:
                return ObjectListKind.Hierarchy;
            case (uint)_LIB_LISTTYPE.LLT_MEMBERS:
                return ObjectListKind.Members;
            case (uint)_LIB_LISTTYPE.LLT_NAMESPACES:
                return ObjectListKind.Namespaces;
            case (uint)_LIB_LISTTYPE.LLT_PACKAGE:
                return ObjectListKind.Projects;
            case LLT_PROJREF:
                return ObjectListKind.References;
            case (uint)_LIB_LISTTYPE.LLT_USESCLASSES:
                return ObjectListKind.BaseTypes;
        }

        Debug.Fail("Unsupported list type: " + ((_LIB_LISTTYPE)listType).ToString());

        return ObjectListKind.None;
    }

    public static uint ObjectListKindToListType(ObjectListKind kind)
    {
        switch (kind)
        {
            case ObjectListKind.BaseTypes:
                return (uint)_LIB_LISTTYPE.LLT_USESCLASSES;
            case ObjectListKind.Hierarchy:
                return (uint)_LIB_LISTTYPE.LLT_HIERARCHY;
            case ObjectListKind.Members:
                return (uint)_LIB_LISTTYPE.LLT_MEMBERS;
            case ObjectListKind.Namespaces:
                return (uint)_LIB_LISTTYPE.LLT_NAMESPACES;
            case ObjectListKind.Projects:
                return (uint)_LIB_LISTTYPE.LLT_PACKAGE;
            case ObjectListKind.References:
                return LLT_PROJREF;
            case ObjectListKind.Types:
                return (uint)_LIB_LISTTYPE.LLT_CLASSES;
        }

        Debug.Fail("Unsupported object list kind: " + kind.ToString());

        return 0;
    }

    public const _LIB_LISTFLAGS ClassView = _LIB_LISTFLAGS.LLF_TRUENESTING;

    public static bool IsClassView(uint flags)
        => (flags & (uint)_LIB_LISTFLAGS.LLF_TRUENESTING) != 0;

    public static bool IsFindSymbol(uint flags)
        => (flags & (uint)_LIB_LISTFLAGS.LLF_USESEARCHFILTER) != 0;

    internal static bool IsObjectBrowser(uint flags)
        => (flags & ((uint)_LIB_LISTFLAGS.LLF_TRUENESTING | (uint)_LIB_LISTFLAGS.LLF_USESEARCHFILTER | (uint)_LIB_LISTFLAGS.LLF_RESOURCEVIEW)) == 0;
}
