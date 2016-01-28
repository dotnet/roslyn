// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class ServiceProviderExtensions
    {
        public static TInterface GetService<TService, TInterface>(this IServiceProvider serviceProvider)
        {
            var service = (TInterface)serviceProvider.GetService(typeof(TService));
            Debug.Assert(service != null);
            return service;
        }
    }
}
