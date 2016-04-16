// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal enum ConsoleColor
    {
        Black = 0,
        DarkBlue = 1,
        DarkGreen = 2,
        DarkCyan = 3,
        DarkRed = 4,
        DarkMagenta = 5,
        DarkYellow = 6,
        Gray = 7,
        DarkGray = 8,
        Blue = 9,
        Green = 10,
        Cyan = 11,
        Red = 12,
        Magenta = 13,
        Yellow = 14,
        White = 15
    }

    internal static class ConsoleShims
    {
        private const string ConsoleTypeName = "System.Console";
        private const string ConsoleColorTypeName = "System.ConsoleColor";
        private const string System_Console = "System.Console, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        private static readonly Type s_ConsoleType = ReflectionUtilities.GetTypeFromEither(contractName: $"{ConsoleTypeName}, {System_Console}", desktopName: ConsoleTypeName);
        private static readonly Type s_ConsoleColorType = ReflectionUtilities.GetTypeFromEither(contractName: $"{ConsoleColorTypeName}, {System_Console}", desktopName: ConsoleColorTypeName);

        private static readonly PropertyInfo s_ForegroundColorProperty = s_ConsoleType.GetTypeInfo().GetDeclaredProperty("ForegroundColor");
        private static readonly PropertyInfo s_OutProperty = s_ConsoleType.GetTypeInfo().GetDeclaredProperty("Out");
        private static readonly PropertyInfo s_InProperty = s_ConsoleType.GetTypeInfo().GetDeclaredProperty("In");
        private static readonly PropertyInfo s_ErrorProperty = s_ConsoleType.GetTypeInfo().GetDeclaredProperty("Error");
        private static readonly Action s_resetColor = s_ConsoleType.GetTypeInfo().GetDeclaredMethod("ResetColor").CreateDelegate<Action>();

        public static ConsoleColor ForegroundColor
        {
            set
            {
                s_ForegroundColorProperty.SetValue(null, Enum.ToObject(s_ConsoleColorType, (int)value));
            }
        }

        public static TextReader In => (TextReader)s_InProperty.GetValue(null);
        public static TextWriter Out => (TextWriter)s_OutProperty.GetValue(null);
        public static TextWriter Error => (TextWriter)s_ErrorProperty.GetValue(null);
        public static void ResetColor() => s_resetColor();
    }
}
