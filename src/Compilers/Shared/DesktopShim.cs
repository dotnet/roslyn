// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This is a bridge for APIs that are only available on Desktop 
    /// and NOT on CoreCLR. The compiler currently targets .NET 4.5 and CoreCLR
    /// so this shim is necessary for switching on the dependent behavior.
    /// </summary>
    internal static class DesktopShim
    {
        internal static class FileNotFoundExceptionShim
        {
            internal static readonly Type Type = typeof(FileNotFoundException);

            private static readonly PropertyInfo s_fusionLog = Type.GetTypeInfo().GetDeclaredProperty("FusionLog");

            internal static string TryGetFusionLog(object obj) => s_fusionLog?.GetValue(obj) as string;
        }
    }
}
