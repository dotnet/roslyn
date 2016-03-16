// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    internal class IntegrationService : MarshalByRefObject
    {
        public static readonly string PortNameFormatString = $"{nameof(IntegrationService)}_{{0}}";

        private ConcurrentDictionary<string, ObjRef> _marshalledObjects = new ConcurrentDictionary<string, ObjRef>();

        public string Execute(string assemblyFilePath, string typeFullName, string methodName, BindingFlags bindingFlags, params object[] parameters)
        {
            var assembly = Assembly.LoadFrom(assemblyFilePath);
            var type = assembly.GetType(typeFullName);
            var methodInfo = type.GetMethod(methodName, bindingFlags);
            var result = methodInfo.Invoke(null, parameters);

            if (methodInfo.ReturnType == typeof(void))
            {
                return null;
            }

            var resultType = result.GetType();
            var marshallableResult = (MarshalByRefObject)(result);
            var objectUri = $"{resultType.FullName}_{Guid.NewGuid()}";
            var marshalledObject = RemotingServices.Marshal(marshallableResult, objectUri, resultType);

            if (!_marshalledObjects.TryAdd(objectUri, marshalledObject))
            {
                throw new InvalidOperationException($"An object with the specified URI has already been marshalled. (URI: {objectUri})");
            }

            return objectUri;
        }
    }
}
