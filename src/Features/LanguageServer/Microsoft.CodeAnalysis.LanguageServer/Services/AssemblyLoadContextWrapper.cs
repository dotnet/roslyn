// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Services
{
    internal sealed class AssemblyLoadContextWrapper : IDisposable
    {

        // WORKAROUND for Roslyn bugs 71100 and 71101
        private static class AssemblyLoadHelper
        {
            private static readonly ConcurrentDictionary<string, Assembly?> loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);
            static AssemblyLoadHelper()
            {
                // We might need to load System.Xaml for extensibility *.DesignTools.dll
                Assembly? ResolveAssembly(object sender, ResolveEventArgs args)
                {
                    string name = new AssemblyName(args.Name).Name;
                    if (name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    if (name.Equals("System.Xaml", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("Microsoft.VisualStudio.Telemetry", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Microsoft.VisualStudio.DesignTools.", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics", StringComparison.OrdinalIgnoreCase))
                    {
                        Assembly? assembly = loadedAssemblies.GetOrAdd(name, AssemblyLoadHelper.EnsureAssembly);
                        return assembly;
                    }

                    return null;
                }

                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            }

            private static Assembly? EnsureAssembly(string name)
            {
                if (AssemblyLoadHelper.GetLoadedAssembly(name) is not Assembly assembly)
                {
                    string path = Assembly.GetExecutingAssembly().Location;
                    string folder = Path.GetDirectoryName(path);
                    string assemblyPath = Path.Combine(folder, name + ".dll");
                    if (File.Exists(assemblyPath))
                    {
                        assembly = Assembly.LoadFrom(assemblyPath);
                    }
                    else
                    {
                        Debug.Fail($"Missing {assemblyPath}");
                        return null;
                    }
                }

                return assembly;
            }

            private static Assembly? GetLoadedAssembly(string name)
            {
                // Find existing assembly
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly loadedAssembly in assemblies)
                {
                    AssemblyName loadedAssemblyName = loadedAssembly.GetName();
                    if (string.Equals(loadedAssemblyName.Name, name, StringComparison.Ordinal))
                    {
                        return loadedAssembly;
                    }
                }

                return null;
            }
        }

        static AssemblyLoadContextWrapper()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(AssemblyLoadHelper).TypeHandle);
        }
        // WORKAROUND - END

        private AssemblyLoadContext? _assemblyLoadContext;
        private readonly ImmutableDictionary<string, Assembly> _loadedAssemblies;
        private readonly ILogger? _logger;

        private AssemblyLoadContextWrapper(AssemblyLoadContext assemblyLoadContext, ImmutableDictionary<string, Assembly> loadedFiles, ILogger? logger)
        {
            _assemblyLoadContext = assemblyLoadContext;
            _loadedAssemblies = loadedFiles;
            _logger = logger;
        }

        public static bool TryLoadExtension(string assemblyFilePath, ILogger logger, [NotNullWhen(true)] out Assembly? assembly)
        {
            var dir = Path.GetDirectoryName(assemblyFilePath);
            var fileName = Path.GetFileName(assemblyFilePath);
            var fileNameNoExt = Path.GetFileNameWithoutExtension(assemblyFilePath);

            Contract.ThrowIfNull(dir);
            Contract.ThrowIfNull(fileName);
            Contract.ThrowIfNull(fileNameNoExt);

            var loadContext = TryCreate(fileNameNoExt, dir, logger);
            if (loadContext != null)
            {
                assembly = loadContext.GetAssembly(fileName);
                return true;
            }

            assembly = null;
            return false;
        }

        public static AssemblyLoadContextWrapper? TryCreate(string name, string assembliesDirectoryPath, ILogger logger)
        {
            try
            {
                logger.LogTrace("[{name}] Loading assemblies in {assembliesDirectoryPath}", name, assembliesDirectoryPath);

                var loadContext = new AssemblyLoadContext(name);
                var directory = new DirectoryInfo(assembliesDirectoryPath);
                var builder = new Dictionary<string, Assembly>();
                foreach (var file in directory.GetFiles("*.dll"))
                {
                    logger.LogTrace("[{name}] Loading {assemblyName}", loadContext.Name, file.Name);
                    builder.Add(file.Name, loadContext.LoadFromAssemblyPath(file.FullName));
                }

                return new AssemblyLoadContextWrapper(loadContext, builder.ToImmutableDictionary(), logger);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to initialize AssemblyLoadContext {name}", name);
                return null;
            }
        }

        public Assembly GetAssembly(string name) => _loadedAssemblies[name];

        public MethodInfo? TryGetMethodInfo(string assemblyName, string className, string methodName)
        {
            try
            {
                return GetMethodInfo(assemblyName, className, methodName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get method information from {assembly} for {class}.{method}", assemblyName, className, methodName);
                return null;
            }
        }

        public MethodInfo GetMethodInfo(string assemblyName, string className, string methodName)
        {
            var assembly = GetAssembly(assemblyName);
            var completionHelperType = assembly.GetType(className);
            if (completionHelperType == null)
            {
                throw new ArgumentException($"{assembly.FullName} assembly did not contain {className} class");
            }
            var createCompletionProviderMethodInto = completionHelperType?.GetMethod(methodName);
            if (createCompletionProviderMethodInto == null)
            {
                throw new ArgumentException($"{className} from {assembly.FullName} assembly did not contain {methodName} method");
            }
            return createCompletionProviderMethodInto;
        }

        public void Dispose()
        {
            _assemblyLoadContext?.Unload();
            _assemblyLoadContext = null;
        }

        private sealed class AssemblyNameComparer : IEqualityComparer<AssemblyName>
        {
            public static readonly AssemblyNameComparer Default = new();

            public bool Equals(AssemblyName? x, AssemblyName? y)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (x == null || y == null)
                    return false;

                return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.CultureName, y.CultureName, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(AssemblyName obj)
                => HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CultureName ?? string.Empty));
        }
    }
}
