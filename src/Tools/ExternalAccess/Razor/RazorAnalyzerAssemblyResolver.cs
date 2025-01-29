// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET

using System;
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
        private const string RazorCompilerAssemblyName = "Microsoft.CodeAnalysis.Razor.Compiler";
        private const string RazorUtilsAssemblyName = "Microsoft.AspNetCore.Razor.Utilities.Shared";
        private const string ObjectPoolAssemblyName = "Microsoft.Extensions.ObjectPool";

        public Assembly? Resolve(AnalyzerAssemblyLoader loader, AssemblyLoadContext current, AssemblyName assemblyName, string directory)
        {
            if (assemblyName.Name is not (RazorCompilerAssemblyName or RazorUtilsAssemblyName or ObjectPoolAssemblyName))
            {
                return null;
            }

            var compilerContext = AssemblyLoadContext.GetLoadContext(typeof(Compilation).Assembly)!;
            if (compilerContext.Assemblies.SingleOrDefault(a => a.GetName().Name == assemblyName.Name) is Assembly loadedAssembly)
            {
                return loadedAssembly;
            }

            ReadOnlySpan<string> razorAssemblies = [RazorCompilerAssemblyName, RazorUtilsAssemblyName, ObjectPoolAssemblyName];
            Assembly? result = null;
            foreach (var razorAssemblyName in razorAssemblies)
            {
                var assemblyPath = Path.Combine(directory, $"{razorAssemblyName}.dll");
                var assembly = compilerContext.LoadFromAssemblyPath(assemblyPath);
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
