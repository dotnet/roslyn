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
        public const string RazorCompilerAssemblyName = "Microsoft.CodeAnalysis.Razor.Compiler";
        public const string RazorUtilsAssemblyName = "Microsoft.AspNetCore.Razor.Utilities.Shared";
        public const string ObjectPoolAssemblyName = "Microsoft.Extensions.ObjectPool";

        internal const string ServiceHubCoreFolderName = "ServiceHubCore";

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

            // https://github.com/dotnet/roslyn/issues/76868
            // load the complete closure of razor assemblies if we're asked to load any of them. Subsequent requests for the others will just return the ones loaded here
            LoadAssemblyByFileName(compilerLoadContext, RazorCompilerAssemblyName, directory);
            LoadAssemblyByFileName(compilerLoadContext, RazorUtilsAssemblyName, directory);
            LoadAssemblyByFileName(compilerLoadContext, ObjectPoolAssemblyName, directory);

            // return the actual assembly that we were asked to load.
            return LoadAssembly(compilerLoadContext, assemblyName, directory);

            static Assembly? LoadAssemblyByFileName(AssemblyLoadContext compilerLoadContext, string fileName, string directory)
            {
                // This is kind of odd that we find the assembly on disk, read it to get its assemblyName, then load it by assemblyName,
                // which in turn attempts to find it on the disk, but ensures we go through the correct loading logic later on.
                var onDiskName = Path.Combine(directory, $"{fileName}.dll");
                if (File.Exists(onDiskName))
                {
                    return LoadAssembly(compilerLoadContext, AssemblyName.GetAssemblyName(onDiskName), directory);
                }
                return null;
            }

            static Assembly? LoadAssembly(AssemblyLoadContext compilerLoadContext, AssemblyName assemblyName, string directory)
            {
                var assembly = compilerLoadContext.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
                if (assembly is not null)
                {
                    return assembly;
                }

                var assemblyFileName = $"{assemblyName.Name}.dll";

                // Depending on who wins the race to load these assemblies, the base directory will either be the tooling root (if Roslyn wins)
                // or the ServiceHubCore subfolder (razor). In the root directory these are netstandard2.0 targeted, in ServiceHubCore they are 
                // .net targeted. We need to always pick the same set of assemblies regardless of who causes us to load. Because this code only
                // runs in a .net based host, it's safe to always choose the .net targeted ServiceHubCore versions.
                if (!Path.GetFileName(directory.AsSpan().TrimEnd(Path.DirectorySeparatorChar)).Equals(ServiceHubCoreFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    var serviceHubCoreDirectory = Path.Combine(directory, ServiceHubCoreFolderName);

                    // The logic above only applies to VS. In VS Code there is no service hub, so appending the folder would be silly.
                    if (Directory.Exists(serviceHubCoreDirectory))
                    {
                        directory = serviceHubCoreDirectory;
                    }
                }

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
}
#endif
