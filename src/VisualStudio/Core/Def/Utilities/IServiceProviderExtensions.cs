// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class IServiceProviderExtensions
    {
        /// <summary>
        /// Returns the specified interface from the service. This is useful when the service and interface differ
        /// </summary>
        public static TInterfaceType GetService<TInterfaceType, TServiceType>(this IServiceProvider sp)
            where TInterfaceType : class
            where TServiceType : class
        {
            return (TInterfaceType)sp.GetService(typeof(TServiceType));
        }

        /// <summary>
        /// Returns the specified service type from the service.
        /// </summary>
        public static TServiceType GetService<TServiceType>(this IServiceProvider sp) where TServiceType : class
        {
            return sp.GetService<TServiceType, TServiceType>();
        }
    }
}
