// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This is a bridge for APIs that are only available on CoreCLR or .NET 4.6
    /// and NOT on .NET 4.5. The compiler currently targets .NET 4.5 and CoreCLR
    /// so this shim is necessary for switching on the dependent behavior.
    /// </summary>
    internal static class DesktopShim
    {
        internal static class FileNotFoundException
        {
            internal static readonly Type Type = ReflectionUtilities.TryGetType(
               "System.IO.FileNotFoundException, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            private static PropertyInfo s_fusionLog = Type?.GetTypeInfo().GetDeclaredProperty("FusionLog");

            internal static string TryGetFusionLog(object obj) => s_fusionLog.GetValue(obj) as string;
        }
    }
}
