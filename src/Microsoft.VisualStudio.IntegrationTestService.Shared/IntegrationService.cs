// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.IntegrationTestService
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
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
            var uri = $"ipc://{GetPortName(hostProcess.Id)}/{typeof(IntegrationService).FullName}";
            return (IntegrationService)Activator.GetObject(typeof(IntegrationService), uri);
        }

        public string Execute(string assemblyFilePath, string typeFullName, string methodName)
        {
            var assembly = Assembly.LoadFrom(assemblyFilePath);
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
    }
}
