// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;
using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Shim for APIs available only on CoreCLR.
    /// </summary>
    internal static class CoreClrShim
    {
        internal static bool IsRunningOnCoreClr => AssemblyLoadContext.Type != null;

        internal static class AssemblyLoadContext
        {
            internal static readonly Type Type = ReflectionUtilities.TryGetType(
               "System.Runtime.Loader.AssemblyLoadContext, System.Runtime.Loader, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        }

        internal static class AppContext
        {
            internal static readonly Type Type = ReflectionUtilities.TryGetType(
                "System.AppContext, System.AppContext, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            // only available in netstandard 1.6+
            internal static readonly Func<string, object> GetData =
                Type.GetTypeInfo().GetDeclaredMethod("GetData")?.CreateDelegate<Func<string, object>>();
        }
    }
}
