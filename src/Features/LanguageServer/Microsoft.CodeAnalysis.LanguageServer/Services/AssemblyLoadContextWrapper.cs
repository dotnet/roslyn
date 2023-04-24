// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Services
{
    internal class AssemblyLoadContextWrapper : IDisposable
    {
        private AssemblyLoadContext? _assemblyLoadContext;
        private readonly ImmutableDictionary<string, Assembly> _loadedAssemblies;
        private readonly ILogger? _logger;

        private AssemblyLoadContextWrapper(AssemblyLoadContext assemblyLoadContext, ImmutableDictionary<string, Assembly> loadedFiles, ILogger? logger)
        {
            _assemblyLoadContext = assemblyLoadContext;
            _loadedAssemblies = loadedFiles;
            _logger = logger;
        }

        public static AssemblyLoadContextWrapper? TryCreate(string name, string assembliesDirectoryPath, ILogger? logger)
        {
            try
            {
                var alc = new AssemblyLoadContext(name);
                var directory = new DirectoryInfo(assembliesDirectoryPath);
                var builder = new Dictionary<string, Assembly>();
                foreach (var file in directory.GetFiles("*.dll"))
                {
                    builder.Add(file.Name, alc.LoadFromAssemblyPath(file.FullName));
                }

                return new AssemblyLoadContextWrapper(alc, builder.ToImmutableDictionary(), logger);
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
    }
}
