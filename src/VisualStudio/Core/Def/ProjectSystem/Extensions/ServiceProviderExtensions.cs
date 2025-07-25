// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

internal static class ServiceProviderExtensions
{
    extension(IServiceProvider serviceProvider)
    {
        public T GetMefService<T>() where T : class
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            return componentModel.GetService<T>();
        }
    }
}
