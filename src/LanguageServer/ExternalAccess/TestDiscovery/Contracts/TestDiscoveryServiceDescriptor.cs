// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.TestDiscovery.Contracts;

/// <summary>
/// Identifies the test discovery brokered service.
/// <para>
/// The <see cref="Moniker"/> is the single stable coupling point between Roslyn and the C# Dev Kit
/// client. Roslyn registers and proffers a service under this descriptor.
/// </para>
/// </summary>
internal static class TestDiscoveryServiceDescriptor
{
    /// <summary>The brokered service moniker name. Must match the C# Dev Kit client descriptor.</summary>
    public const string MonikerName = "Microsoft.VisualStudio.CSharpDevKit.SourceTestDiscoveryService";

    /// <summary>The brokered service moniker version. Must match the C# Dev Kit client descriptor.</summary>
    public const string MonikerVersion = "0.1";

    public static readonly ServiceMoniker Moniker = new(MonikerName, new Version(MonikerVersion));

    /// <summary>The shared descriptor used to proffer (host) and acquire (client) the service.</summary>
	public static readonly ServiceRpcDescriptor Descriptor =
        new ServiceJsonRpcDescriptor(
            Moniker,
            ServiceJsonRpcDescriptor.Formatters.MessagePack,
            ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader
);
}
