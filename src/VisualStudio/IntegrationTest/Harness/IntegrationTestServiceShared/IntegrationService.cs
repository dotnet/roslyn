// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTestService
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Remoting;

    /// <summary>
    /// Provides a means of executing code in the Visual Studio host process.
    /// </summary>
    /// <remarks>
    /// This object exists in the Visual Studio host and is marshaled across the process boundary.
    /// </remarks>
    public class IntegrationService : MarshalByRefObject
    {
        private static readonly ConcurrentDictionary<string, byte> s_codeBaseDirectories = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, ObjRef> _marshaledObjects = new ConcurrentDictionary<string, ObjRef>();

        public IntegrationService()
        {
            PortName = GetPortName(Process.GetCurrentProcess().Id);
            BaseUri = "ipc://" + PortName;
        }

        public string PortName
        {
            get;
        }

        /// <summary>
        /// Gets the base Uri of the service. This resolves to a string such as <c>ipc://IntegrationService_{HostProcessId}"</c>.
        /// </summary>
        public string BaseUri
        {
            get;
        }

        private static string GetPortName(int hostProcessId)
        {
            // Make the channel name well-known by using a static base and appending the process ID of the host
            return $"{nameof(IntegrationService)}_{{{hostProcessId}}}";
        }

        public static IntegrationService GetInstanceFromHostProcess(Process hostProcess)
        {
            if (hostProcess is null)
            {
                throw new ArgumentNullException(nameof(hostProcess));
            }

            var uri = $"ipc://{GetPortName(hostProcess.Id)}/{typeof(IntegrationService).FullName}";
            return (IntegrationService)Activator.GetObject(typeof(IntegrationService), uri);
        }

        public string? Execute(string assemblyFilePath, string typeFullName, string methodName)
        {
            AddCodeBaseDirectory(Path.GetDirectoryName(assemblyFilePath));

            var assembly = LoadAssemblyFromPath(assemblyFilePath);
            var type = assembly.GetType(typeFullName);
            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            var result = methodInfo.Invoke(null, null);

            if (methodInfo.ReturnType == typeof(void))
            {
                return null;
            }

            // Create a unique URL for each object returned, so that we can communicate with each object individually
            var resultType = result.GetType();
            var marshallableResult = (MarshalByRefObject)result;
            var objectUri = $"{resultType.FullName}_{Guid.NewGuid()}";
            var marshalledObject = RemotingServices.Marshal(marshallableResult, objectUri, resultType);

            if (!_marshaledObjects.TryAdd(objectUri, marshalledObject))
            {
                throw new InvalidOperationException($"An object with the specified URI has already been marshaled. (URI: {objectUri})");
            }

            return objectUri;
        }

        private static void AddCodeBaseDirectory(string? directory)
        {
            if (directory == null)
            {
                return;
            }

            directory = Path.GetFullPath(directory);
            if (!s_codeBaseDirectories.TryAdd(directory, 0))
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                var assemblyName = new AssemblyName(e.Name);
                var isRoslynProductAssembly = IsRoslynProductAssemblyName(assemblyName.Name);

                var loadedAssembly = isRoslynProductAssembly
                    ? GetRoslynProductAssembly(assemblyName, directory)
                    : GetLoadedAssembly(assemblyName);
                if (loadedAssembly != null)
                {
                    return loadedAssembly;
                }

                if (isRoslynProductAssembly)
                {
                    return null;
                }

                var path = Path.Combine(directory, assemblyName.Name + ".dll");
                if (File.Exists(path))
                {
                    return LoadAssemblyFromPath(path);
                }

                return null;
            };
        }

        private static Assembly? GetRoslynProductAssembly(AssemblyName requestedAssemblyName, string codeBaseDirectory)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, requestedAssemblyName.Name, StringComparison.Ordinal)
                    && !IsAssemblyFromDirectory(assembly, codeBaseDirectory))
                {
                    return assembly;
                }
            }

            var baseDirectory = AppContext.BaseDirectory;
            if (baseDirectory != null)
            {
                var path = Path.Combine(
                    baseDirectory,
                    "CommonExtensions",
                    "Microsoft",
                    "VBCSharp",
                    "LanguageServices",
                    requestedAssemblyName.Name + ".dll");
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }
            }

            return null;
        }

        private static Assembly LoadAssemblyFromPath(string path)
            => Assembly.LoadFile(Path.GetFullPath(path));

        private static Assembly? GetLoadedAssembly(AssemblyName requestedAssemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.FullName, requestedAssemblyName.FullName, StringComparison.Ordinal))
                {
                    return assembly;
                }
            }

            return null;
        }

        private static bool IsAssemblyFromDirectory(Assembly assembly, string directory)
        {
            var assemblyLocation = assembly.Location;
            if (assemblyLocation.Length == 0)
            {
                return false;
            }

            var fullAssemblyLocation = Path.GetFullPath(assemblyLocation);
            var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullAssemblyLocation.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRoslynProductAssemblyName(string? assemblyName)
        {
            if (assemblyName == null
                || assemblyName.IndexOf(".Test", StringComparison.Ordinal) >= 0
                || assemblyName.EndsWith("Tests", StringComparison.Ordinal))
            {
                return false;
            }

            return assemblyName == "Microsoft.CodeAnalysis"
                || assemblyName == "Microsoft.VisualStudio.LanguageServices"
                || assemblyName.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal)
                || assemblyName.StartsWith("Microsoft.VisualStudio.LanguageServices.", StringComparison.Ordinal);
        }

        // Ensure in-process components live forever
        public override object? InitializeLifetimeService()
        {
            return null;
        }
    }
}
