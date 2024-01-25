// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This type contains the light up scenarios for various platform and runtimes.  Any function
    /// in this type can, and is expected to, fail on various platforms.  These are light up scenarios
    /// only.
    /// </summary>
    internal static class CoreLightup
    {
        internal static class Desktop
        {
            private static class _Assembly
            {
                internal static readonly Type Type = typeof(Assembly);

                internal static readonly Func<Assembly, bool> get_GlobalAssemblyCache = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("get_GlobalAssemblyCache")
                    .CreateDelegate<Func<Assembly, bool>>();
            }

            private static class _ResolveEventArgs
            {
                internal static readonly Type Type = ReflectionUtilities.TryGetType("System.ResolveEventArgs");

                internal static readonly MethodInfo get_Name = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("get_Name");

                internal static readonly MethodInfo get_RequestingAssembly = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("get_RequestingAssembly");
            }

            private static class _AppDomain
            {
                internal static readonly Type Type = ReflectionUtilities.TryGetType("System.AppDomain");
                internal static readonly Type ResolveEventHandlerType = ReflectionUtilities.TryGetType("System.ResolveEventHandler");

                internal static readonly MethodInfo get_CurrentDomain = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("get_CurrentDomain");

                internal static readonly MethodInfo add_AssemblyResolve = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("add_AssemblyResolve", ResolveEventHandlerType);

                internal static readonly MethodInfo remove_AssemblyResolve = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("remove_AssemblyResolve", ResolveEventHandlerType);
            }

            internal static bool IsAssemblyFromGlobalAssemblyCache(Assembly assembly)
            {
                if (_Assembly.get_GlobalAssemblyCache == null)
                {
                    throw new PlatformNotSupportedException();
                }

                return _Assembly.get_GlobalAssemblyCache(assembly);
            }

            private sealed class AssemblyResolveWrapper
            {
                private readonly Func<string, Assembly, Assembly> _handler;
                private static readonly MethodInfo s_stubInfo = typeof(AssemblyResolveWrapper).GetTypeInfo().GetDeclaredMethod("Stub");

                public AssemblyResolveWrapper(Func<string, Assembly, Assembly> handler)
                {
                    _handler = handler;
                }

#pragma warning disable IDE0051 // Remove unused private members - Reflection (s_stubInfo)
#pragma warning disable IDE0060 // Remove unused parameter
                private Assembly Stub(object sender, object resolveEventArgs)
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0060 // Remove unused parameter
                {
                    var name = (string)_ResolveEventArgs.get_Name.Invoke(resolveEventArgs, Array.Empty<object>());
                    var requestingAssembly = (Assembly)_ResolveEventArgs.get_RequestingAssembly.Invoke(resolveEventArgs, Array.Empty<object>());

                    return _handler(name, requestingAssembly);
                }

                public object GetHandler()
                {
                    return s_stubInfo.CreateDelegate(_AppDomain.ResolveEventHandlerType, this);
                }
            }

            internal static void GetOrRemoveAssemblyResolveHandler(Func<string, Assembly, Assembly> handler, MethodInfo handlerOperation)
            {
                if (_AppDomain.add_AssemblyResolve == null)
                {
                    throw new PlatformNotSupportedException();
                }

                var currentAppDomain = AppDomain.CurrentDomain;
                object resolveEventHandler = new AssemblyResolveWrapper(handler).GetHandler();

                handlerOperation.Invoke(currentAppDomain, [resolveEventHandler]);
            }

            internal static void AddAssemblyResolveHandler(Func<string, Assembly, Assembly> handler)
            {
                GetOrRemoveAssemblyResolveHandler(handler, _AppDomain.add_AssemblyResolve);
            }

            internal static void RemoveAssemblyResolveHandler(Func<string, Assembly, Assembly> handler)
            {
                GetOrRemoveAssemblyResolveHandler(handler, _AppDomain.remove_AssemblyResolve);
            }
        }
    }
}
