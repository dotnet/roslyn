// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;

internal static class ServiceProviderExtensions
{
    extension(IServiceProvider serviceProvider)
    {
        public bool TryGetService<TService, TInterface>(JoinableTaskFactory joinableTaskFactory, [NotNullWhen(true)] out TInterface? @interface)
        where TInterface : class
        {
            @interface = serviceProvider.GetService<TService, TInterface>(joinableTaskFactory, throwOnFailure: false);
            return @interface is not null;
        }

        public bool TryGetService<TInterface>(JoinableTaskFactory joinableTaskFactory, [NotNullWhen(true)] out TInterface? @interface)
                where TInterface : class
        {
            @interface = serviceProvider.GetService<TInterface>(joinableTaskFactory);
            return @interface is not null;
        }

        public TInterface? GetService<TInterface>(JoinableTaskFactory joinableTaskFactory)
            where TInterface : class
        {
            return serviceProvider.GetService<TInterface, TInterface>(joinableTaskFactory, throwOnFailure: false);
        }
    }
}
