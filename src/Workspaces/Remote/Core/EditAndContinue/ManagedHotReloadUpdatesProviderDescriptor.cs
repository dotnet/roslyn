// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static class ManagedHotReloadUpdatesProviderDescriptor
{
    public static readonly ServiceMoniker Moniker = new(
        BrokeredServiceDescriptors.LanguageServerComponentNamespace + "." + BrokeredServiceDescriptors.LanguageServerComponentName + "." + "ManagedHotReloadUpdatesProvider", new(3, 0));
}
