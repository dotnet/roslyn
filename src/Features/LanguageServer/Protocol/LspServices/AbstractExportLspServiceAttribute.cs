// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal abstract class AbstractExportLspServiceAttribute : ExportAttribute
{
    /// <summary>
    /// The full assembly-qualified type name of the service being exported.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The LSP server for which this service applies to.  If null, this service applies to any server
    /// with the matching contract name.
    /// </summary>
    public WellKnownLspServerKinds? ServerKind { get; }

    /// <summary>
    /// Services MEF exported as <see cref="ILspService"/> must by definition be stateless as they are
    /// shared amongst all LSP server instances through restarts.
    /// </summary>
    public bool IsStateless { get; }

    private readonly Lazy<byte[]>? _lazyMethodHandlerDescriptorData;

    /// <summary>
    /// If this this service implements <see cref="IMethodHandler"/>, returns a blob of binary data
    /// that encodes an array of <see cref="MethodHandlerDescriptor"/>s; otherwise <see langword="null"/>.
    /// </summary>
    public byte[]? MethodHandlerDescriptorData => _lazyMethodHandlerDescriptorData?.Value;

    protected AbstractExportLspServiceAttribute(
        Type serviceType, string contractName, Type contractType, bool isStateless, WellKnownLspServerKinds serverKind)
        : base(contractName, contractType)
    {
        Contract.ThrowIfFalse(serviceType.GetInterfaces().Contains(typeof(ILspService)), $"{serviceType.Name} does not inherit from {nameof(ILspService)}");
        Contract.ThrowIfNull(serviceType.AssemblyQualifiedName);

        TypeName = serviceType.AssemblyQualifiedName;
        IsStateless = isStateless;
        ServerKind = serverKind;

        _lazyMethodHandlerDescriptorData = typeof(IMethodHandler).IsAssignableFrom(serviceType)
            ? new(() => MefSerialization.Serialize(HandlerReflection.GetMethodHandlers(serviceType)))
            : null;
    }
}
