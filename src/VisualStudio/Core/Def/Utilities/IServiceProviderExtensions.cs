// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class IServiceProviderExtensions
    {
        /// <summary>
        /// Returns the specified interface from the service. This is useful when the service and interface differ
        /// </summary>
        public static InterfaceType GetService<InterfaceType, ServiceType>(this IServiceProvider sp)
            where InterfaceType : class
            where ServiceType : class
        {
            return (InterfaceType)sp.GetService(typeof(ServiceType));
        }

        /// <summary>
        /// Returns the specified service type from the service.
        /// </summary>
        public static ServiceType GetService<ServiceType>(this IServiceProvider sp) where ServiceType : class
        {
            return sp.GetService<ServiceType, ServiceType>();
        }
    }
}
