// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(IAnalyzerAssemblyResolver)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class RazorAnalyzerAssemblyResolver() : IAnalyzerAssemblyResolver
    {
        internal const string RazorCompilerAssemblyName = "Microsoft.CodeAnalysis.Razor.Compiler";
        internal const string RazorUtilsAssemblyName = "Microsoft.AspNetCore.Razor.Utilities.Shared";
        internal const string ObjectPoolAssemblyName = "Microsoft.Extensions.ObjectPool";

        internal static readonly ImmutableArray<string> RazorAssemblyNames = [RazorCompilerAssemblyName, RazorUtilsAssemblyName, ObjectPoolAssemblyName];

        public Assembly? Resolve(AnalyzerAssemblyLoader loader, AssemblyLoadContext current, AssemblyName assemblyName, string directory)
        {
            if (assemblyName.Name is not (RazorCompilerAssemblyName or RazorUtilsAssemblyName or ObjectPoolAssemblyName))
            {
                return null;
            }

            ReadOnlySpan<string> razorAssemblies = [RazorCompilerAssemblyName, RazorUtilsAssemblyName, ObjectPoolAssemblyName];
            Assembly? result = null;
            foreach (var razorAssemblyName in razorAssemblies)
            {
                var assemblyPath = Path.Combine(directory, $"{razorAssemblyName}.dll");
                var assembly = loader.CompilerLoadContext.Assemblies.FirstOrDefault(a => a.GetName().Name == razorAssemblyName);
                if (assembly is null)
                {
                    assembly = loader.CompilerLoadContext.LoadFromAssemblyPath(assemblyPath);
                }

                if (assemblyName.Name == razorAssemblyName)
                {
                    result = assembly;
                }
            }

            Debug.Assert(result != null);
            return result;
        }
    }
}
#endif
