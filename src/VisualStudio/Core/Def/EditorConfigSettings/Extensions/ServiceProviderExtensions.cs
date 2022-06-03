// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings
{
    internal static class ServiceProviderExtensions
    {
        public static bool TryGetService<TService, TInterface>(this IServiceProvider serviceProvider, JoinableTaskFactory joinableTaskFactory, [NotNullWhen(true)] out TInterface? @interface)
            where TInterface : class
        {
            @interface = serviceProvider.GetService<TService, TInterface>(joinableTaskFactory, throwOnFailure: false);
            return @interface is not null;
        }

        public static bool TryGetService<TInterface>(this IServiceProvider serviceProvider, JoinableTaskFactory joinableTaskFactory, [NotNullWhen(true)] out TInterface? @interface)
                where TInterface : class
        {
            @interface = serviceProvider.GetService<TInterface>(joinableTaskFactory);
            return @interface is not null;
        }

        public static TInterface? GetService<TInterface>(this IServiceProvider serviceProvider, JoinableTaskFactory joinableTaskFactory)
            where TInterface : class
        {
            return serviceProvider.GetService<TInterface, TInterface>(joinableTaskFactory, throwOnFailure: false);
        }
    }
}
