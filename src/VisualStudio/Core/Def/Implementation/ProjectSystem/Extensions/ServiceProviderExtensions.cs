// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
