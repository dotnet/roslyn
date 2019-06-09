// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Provides a means of executing code in the Visual Studio host process.
    /// </summary>
    /// <remarks>
    /// This object exists in the Visual Studio host and is marhsalled across the process boundary.
    /// </remarks>
    internal class IntegrationService : MarshalByRefObject
    {
        public string PortName { get; }

        /// <summary>
        /// The base Uri of the service. This resolves to a string such as <c>ipc://IntegrationService_{HostProcessId}"</c>
        /// </summary>
        public string BaseUri { get; }

        private readonly ConcurrentDictionary<string, ObjRef> _marshalledObjects = new ConcurrentDictionary<string, ObjRef>();

        public IntegrationService()
        {
            PortName = GetPortName(Process.GetCurrentProcess().Id);
            BaseUri = "ipc://" + this.PortName;
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

            if (!_marshalledObjects.TryAdd(objectUri, marshalledObject))
            {
                throw new InvalidOperationException($"An object with the specified URI has already been marshalled. (URI: {objectUri})");
            }

            return objectUri;
        }

        // Ensure InProcComponents live forever
        public override object InitializeLifetimeService()
            => null;
    }
}
