// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class IServiceProviderExtensions
    {
        /// <inheritdoc cref="Shell.ServiceExtensions.GetService{TService, TInterface}(IServiceProvider, bool)"/>
        public static TInterface GetService<TService, TInterface>(this IServiceProvider sp)
        {
            var service = (TInterface)sp.GetService(typeof(TService));
            Debug.Assert(service != null);
            return service;
        }

        /// <summary>
        /// Returns the specified interface from the service. This is useful when the service and interface differ
        /// </summary>
        public static TInterfaceType GetServiceTheWrongWay<TInterfaceType, TServiceType>(this IServiceProvider sp)
            where TInterfaceType : class
            where TServiceType : class
        {
            return (TInterfaceType)sp.GetService(typeof(TServiceType));
        }

        /// <summary>
        /// Returns the specified service type from the service.
        /// </summary>
        public static TServiceType GetService<TServiceType>(this IServiceProvider sp) where TServiceType : class
            => sp.GetServiceTheWrongWay<TServiceType, TServiceType>();
    }
}
