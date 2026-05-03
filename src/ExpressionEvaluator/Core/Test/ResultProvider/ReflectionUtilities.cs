// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Debugger.Clr;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class ReflectionUtilities
    {
        internal static Assembly Load(ImmutableArray<byte> assembly)
        {
            return Assembly.Load(assembly.ToArray());
        }

        internal static object Instantiate(this Type type, params object[] args)
        {
            return Activator.CreateInstance(
                type,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
                binder: null,
                args: args,
                culture: null);
        }

        internal static AssemblyLoadContext Load(this DkmClrRuntimeInstance runtime)
        {
            return new AssemblyLoadContext(runtime.Assemblies);
        }

        internal static AssemblyLoadContext LoadAssemblies(params Assembly[] assemblies)
        {
            return new AssemblyLoadContext(assemblies);
        }

        internal static Assembly[] GetMscorlib(params Assembly[] additionalAssemblies)
        {
            return
            [
                typeof(object).Assembly, // mscorlib.dll
                .. additionalAssemblies,
            ];
        }

        internal static Assembly[] GetMscorlibAndSystemCore(params Assembly[] additionalAssemblies)
        {
            return
            [
                typeof(object).Assembly, // mscorlib.dll
                typeof(Enumerable).Assembly, // System.Core.dll
                .. additionalAssemblies,
            ];
        }

        internal sealed class AssemblyLoadContext : IDisposable
        {
            private readonly AppDomain _appDomain;
            private readonly Assembly[] _assemblies;

            public AssemblyLoadContext(Assembly[] assemblies)
            {
                _appDomain = AppDomain.CurrentDomain;
                _assemblies = assemblies;
                _appDomain.AssemblyResolve += OnAssemblyResolve;
            }

            private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
            {
                var name = args.Name;
                return _assemblies.FirstOrDefault(a => a.FullName == name);
            }

            public void Dispose()
            {
                _appDomain.AssemblyResolve -= OnAssemblyResolve;
            }
        }
    }
}
