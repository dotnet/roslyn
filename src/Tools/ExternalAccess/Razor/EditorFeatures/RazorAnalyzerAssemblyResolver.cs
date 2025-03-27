﻿// Licensed to the .NET Foundation under one or more agreements.
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
        public const string RazorCompilerAssemblyName = "Microsoft.CodeAnalysis.Razor.Compiler";
        public const string RazorUtilsAssemblyName = "Microsoft.AspNetCore.Razor.Utilities.Shared";
        public const string ObjectPoolAssemblyName = "Microsoft.Extensions.ObjectPool";

        internal static readonly ImmutableArray<string> RazorAssemblyNames = [RazorCompilerAssemblyName, RazorUtilsAssemblyName, ObjectPoolAssemblyName];

        public Assembly? Resolve(AnalyzerAssemblyLoader loader, AssemblyName assemblyName, AssemblyLoadContext directoryContext, string directory) =>
            ResolveCore(loader.CompilerLoadContext, assemblyName, directory);

        public static Assembly? ResolveRazorAssembly(AssemblyName assemblyName, string rootDirectory) =>
            ResolveCore(
                AssemblyLoadContext.GetLoadContext(typeof(Microsoft.CodeAnalysis.Compilation).Assembly)!,
                assemblyName,
                rootDirectory);

        /// <summary>
        /// This will resolve the razor generator assembly specified by <paramref name="assemblyName"/> in the specified 
        /// <paramref name="compilerLoadContext"/>.
        /// </summary>
        internal static Assembly? ResolveCore(AssemblyLoadContext compilerLoadContext, AssemblyName assemblyName, string directory)
        {
            if (assemblyName.Name is not (RazorCompilerAssemblyName or RazorUtilsAssemblyName or ObjectPoolAssemblyName))
            {
                return null;
            }

            var assembly = compilerLoadContext.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            if (assembly is not null)
            {
                return assembly;
            }

            var assemblyFileName = $"{assemblyName.Name}.dll";
            var assemblyPath = Path.Combine(directory, assemblyFileName);
            if (File.Exists(assemblyPath))
            {
                // https://github.com/dotnet/roslyn/issues/76868
                //
                // There is a subtle race condition in this logic as another thread could load the assembly in between 
                // the above calls and this one. Short term will just catch and grab the loaded assembly but longer 
                // term need to think about creating a dedicated AssemblyLoadContext for the razor assemblies 
                // which avoids this race condition.
                try
                {
                    assembly = compilerLoadContext.LoadFromAssemblyPath(assemblyPath);
                }
                catch
                {
                    assembly = compilerLoadContext.Assemblies.Single(a => a.GetName().Name == assemblyName.Name);
                }
            }
            else
            {
                // There are assemblies in the razor sdk generator directory that do not exist in the VS installation. That
                // means when the paths are redirected, it's possible that the assembly is not found. In that case, we should
                // load the assembly from the VS installation by querying through the compiler context.
                try
                {
                    assembly = compilerLoadContext.LoadFromAssemblyName(assemblyName);
                }
                catch (FileNotFoundException)
                {
                    assembly = null;
                }
            }

            return assembly;
        }
    }
}
#endif
