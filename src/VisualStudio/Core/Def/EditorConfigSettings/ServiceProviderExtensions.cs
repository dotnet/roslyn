// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings
{
    internal static class ServiceProviderExtensions
    {
        public static bool TryGetService<TService, TInterface>(this IServiceProvider sp, [NotNullWhen(true)] out TInterface? @interface)
            where TInterface : class
        {
            @interface = sp.GetService<TService, TInterface>(throwOnFailure: false);
            return @interface is not null;
        }

        public static bool TryGetService<TInterface>(this IServiceProvider sp, [NotNullWhen(true)] out TInterface? @interface)
                where TInterface : class
        {
            @interface = sp.GetService<TInterface>();
            return @interface is not null;
        }

        public static TInterface? GetService<TInterface>(this IServiceProvider sp)
            where TInterface : class
        {
            return sp.GetService<TInterface, TInterface>(throwOnFailure: false);
        }
    }
}
