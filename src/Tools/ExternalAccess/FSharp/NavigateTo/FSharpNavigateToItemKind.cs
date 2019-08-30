// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo
{
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
}
