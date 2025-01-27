// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET

using System;
using System.Composition;
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

        private static readonly object s_loaderLock = new();

        public static Assembly? ResolveRazorAssembly(AssemblyName assemblyName, string rootDirectory)
        {
            if (assemblyName.Name is RazorCompilerAssemblyName or RazorUtilsAssemblyName or ObjectPoolAssemblyName)
            {
                lock (s_loaderLock)
                {
                    var compilerContext = AssemblyLoadContext.GetLoadContext(typeof(Compilation).Assembly)!;
                    if (compilerContext.Assemblies.SingleOrDefault(a => a.GetName().Name == assemblyName.Name) is Assembly loadedAssembly)
                    {
                        return loadedAssembly;
                    }

                    var assembly = Path.Combine(rootDirectory, $"{assemblyName.Name}.dll");
                    return compilerContext.LoadFromAssemblyPath(assembly);
                }
            }
            return null;
        }

        public Assembly? ResolveAssembly(AssemblyName assemblyName, string directoryName) => ResolveRazorAssembly(assemblyName, directoryName);
    }
}
#endif
