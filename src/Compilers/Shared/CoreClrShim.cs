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
    internal static class CoreClrShim
    {
        internal static class AssemblyLoadContext
        {
            internal static readonly Type Type = ReflectionUtilities.TryGetType(
               "System.Runtime.Loader.AssemblyLoadContext, System.Runtime.Loader, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        }

        internal static class CodePagesEncodingProvider
        {
            internal static readonly Type Type = ReflectionUtilities.TryGetType(
                "System.Text.CodePagesEncodingProvider, " +
                "System.Text.Encoding.CodePages, " +
                "Version=4.0.0.0, Culture=neutral, " +
                "PublicKeyToken=b03f5f7f11d50a3a");

            private static PropertyInfo s_instance = Type
                ?.GetTypeInfo()
                .GetDeclaredProperty("Instance");

            internal static object Instance => s_instance?.GetValue(null);
        }

        internal static class Encoding
        {
            private static readonly MethodInfo s_registerProvider = PortableShim.Encoding.Type
                .GetTypeInfo()
                .GetDeclaredMethod("RegisterProvider");

            internal static void RegisterProvider(object provider)
            {
                try
                {
                    s_registerProvider.Invoke(null, new[] { provider });
                }
                catch (TargetInvocationException e)
                {
                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                }
            }
        }
    }
}
