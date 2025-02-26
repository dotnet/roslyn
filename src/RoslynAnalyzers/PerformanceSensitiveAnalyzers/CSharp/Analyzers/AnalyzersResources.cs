// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers
{
    internal partial class AnalyzersResources
    {
        private static readonly Type s_resourcesType = typeof(AnalyzersResources);

        public static LocalizableResourceString CreateLocalizableResourceString(string nameOfLocalizableResource)
            => new(nameOfLocalizableResource, ResourceManager, s_resourcesType);

        public static LocalizableResourceString CreateLocalizableResourceString(string nameOfLocalizableResource, params string[] formatArguments)
            => new(nameOfLocalizableResource, ResourceManager, s_resourcesType, formatArguments);
    }
}
