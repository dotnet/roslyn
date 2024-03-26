// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.ServiceHub.Framework;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static class ManagedHotReloadLanguageServiceDescriptor
{
    private const string ServiceName = "ManagedHotReloadLanguageService";
    public const string ServiceVersion = "1.0";
    public const string MonikerName = BrokeredServiceDescriptors.LanguageServerComponentNamespace + "." + BrokeredServiceDescriptors.LanguageServerComponentName + "." + ServiceName;

    public static readonly ServiceJsonRpcDescriptor DevKitDescriptor = BrokeredServiceDescriptors.CreateServerServiceDescriptor(ServiceName, new(ServiceVersion));
    public static readonly ServiceJsonRpcDescriptor VisualStudioDescriptor = ServiceDescriptor.CreateInProcServiceDescriptor(ServiceDescriptors.ComponentName, ServiceName, suffix: "", ServiceDescriptors.GetFeatureDisplayName);

    static ManagedHotReloadLanguageServiceDescriptor()
        => Debug.Assert(DevKitDescriptor.Moniker.Name == MonikerName);
}
