// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Exports an <see cref="ILspServiceFactory"/> that is used by LSP server instances
/// to create new instances of the <see cref="ILspService"/> each time an LSP server is started.
/// 
/// The services created by the <see cref="ILspServiceFactory"/> are disposed of by <see cref="LspServices"/>
/// when the LSP server instance shuts down.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false), MetadataAttribute]
internal class ExportLspServiceFactoryAttribute : ExportAttribute
{
    /// <summary>
    /// The assembly-qualified type name of the service being exported.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The LSP server for which this service applies to.  If null, this service applies to any server
    /// with the matching contract name.
    /// </summary>
    public WellKnownLspServerKinds ServerKind { get; }

    /// <summary>
    /// Services MEF exported as <see cref="ILspServiceFactory"/> are stateful as <see cref="LspServices"/>
    /// creates a new instance for each server instance.
    /// </summary>
    public bool IsStateless { get; } = false;

    private readonly Lazy<byte[]>? _lazyMethodHandlerDescriptorData;

    /// <summary>
    /// If this this service implements <see cref="IMethodHandler"/>, returns a blob of binary data
    /// that encodes an array of <see cref="MethodHandlerDescriptor"/>s; otherwise <see langword="null"/>.
    /// </summary>
    public byte[]? MethodHandlerDescriptorData => _lazyMethodHandlerDescriptorData?.Value;

    public ExportLspServiceFactoryAttribute(Type type, string contractName, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any)
        : base(contractName, typeof(ILspServiceFactory))
    {
        Contract.ThrowIfFalse(type.GetInterfaces().Contains(typeof(ILspService)), $"{type.Name} does not inherit from {nameof(ILspService)}");
        Contract.ThrowIfNull(type.AssemblyQualifiedName);

        TypeName = type.AssemblyQualifiedName;
        ServerKind = serverKind;

        _lazyMethodHandlerDescriptorData = typeof(IMethodHandler).IsAssignableFrom(type)
            ? new Lazy<byte[]>(() => MefSerialization.Serialize(HandlerReflection.GetMethodHandlers(type)))
            : null;
    }
}
