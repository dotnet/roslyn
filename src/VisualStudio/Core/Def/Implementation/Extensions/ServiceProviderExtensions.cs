// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
