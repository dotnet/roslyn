// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static class ManagedHotReloadLanguageServiceDescriptor
{
    private const string ServiceName = "ManagedHotReloadLanguageService";
    public const string ServiceVersion = "0.1";
    public const string MonikerName = BrokeredServiceDescriptors.LanguageServerComponentNamespace + "." + BrokeredServiceDescriptors.LanguageServerComponentName + "." + ServiceName;

    public static readonly ServiceJsonRpcDescriptor Descriptor = BrokeredServiceDescriptors.CreateServerServiceDescriptor(ServiceName, new(ServiceVersion));

    static ManagedHotReloadLanguageServiceDescriptor()
        => Debug.Assert(Descriptor.Moniker.Name == MonikerName);
}
