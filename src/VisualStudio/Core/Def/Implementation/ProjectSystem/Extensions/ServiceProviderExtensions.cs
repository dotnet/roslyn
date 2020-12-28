// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions
{
    internal static class ServiceProviderExtensions
    {
        public static T GetMefService<T>(this IServiceProvider serviceProvider) where T : class
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            return componentModel.GetService<T>();
        }
    }
}
