// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Roslyn.VisualStudio.Test.Utilities
{
    internal static class IntegrationServiceExtensions
    {
        public static void Execute(this IntegrationService integrationService, Type type, string methodName, BindingFlags bindingFlags = (BindingFlags.Public | BindingFlags.Static), params object[] parameters)
            => Execute(integrationService, type.Assembly.Location, type.FullName, methodName, bindingFlags, parameters);

        public static T Execute<T>(this IntegrationService integrationService, Type type, string methodName, BindingFlags bindingFlags = (BindingFlags.Public | BindingFlags.Static), params object[] parameters)
            => Execute<T>(integrationService, type.Assembly.Location, type.FullName, methodName, bindingFlags, parameters);

        public static void Execute(this IntegrationService integrationService, string assemblyFilePath, string typeFullName, string methodName, BindingFlags bindingFlags = (BindingFlags.Public | BindingFlags.Static), params object[] parameters)
        {
            var result = integrationService.Execute(assemblyFilePath, typeFullName, methodName, bindingFlags, parameters);

            if (result != null)
            {
                throw new InvalidOperationException("The specified call was not expected to return a value.");
            }
        }

        public static T Execute<T>(this IntegrationService integrationService, string assemblyFilePath, string typeFullName, string methodName, BindingFlags bindingFlags = (BindingFlags.Public | BindingFlags.Static), params object[] parameters)
        {
            var objectUri = integrationService.Execute(assemblyFilePath, typeFullName, methodName, bindingFlags, parameters);

            if (objectUri == null)
            {
                throw new InvalidOperationException("The specified call was expected to return a value.");
            }

            return (T)(Activator.GetObject(typeof(T), $"{integrationService.Uri}/{objectUri}"));
        }
    }
}
