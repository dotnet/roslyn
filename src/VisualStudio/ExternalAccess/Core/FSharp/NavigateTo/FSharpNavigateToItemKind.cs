// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.NavigateTo;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.NavigateTo;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
#endif

internal static class FSharpNavigateToItemKind
{
    public static string Line => NavigateToItemKind.Line;
    public static string File = NavigateToItemKind.File;
    public static string Class => NavigateToItemKind.Class;
    public static string Structure => NavigateToItemKind.Structure;
    public static string Interface => NavigateToItemKind.Interface;
    public static string Delegate => NavigateToItemKind.Delegate;
    public static string Enum => NavigateToItemKind.Enum;
    public static string Module => NavigateToItemKind.Module;
    public static string Constant => NavigateToItemKind.Constant;
    public static string EnumItem => NavigateToItemKind.EnumItem;
    public static string Field => NavigateToItemKind.Field;
    public static string Method => NavigateToItemKind.Method;
    public static string Property => NavigateToItemKind.Property;
    public static string Event => NavigateToItemKind.Event;
    public static string OtherSymbol => NavigateToItemKind.OtherSymbol;
}
