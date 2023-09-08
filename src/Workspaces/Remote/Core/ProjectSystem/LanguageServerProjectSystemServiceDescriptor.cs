// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Remote.ProjectSystem
{
    internal static class LanguageServerProjectSystemServiceDescriptor
    {
        public const string ServiceName = "LanguageServerProjectSystemService";
        public static readonly ServiceDescriptor ServiceDescriptor = ServiceDescriptor.CreateInProcServiceDescriptor(ServiceDescriptors.ComponentName, ServiceName, suffix: "", ServiceDescriptors.GetFeatureDisplayName);
    }
}
