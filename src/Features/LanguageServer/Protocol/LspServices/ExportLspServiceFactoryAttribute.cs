// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
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
    public string AssemblyQualifiedName { get; }

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

    /// <summary>
    /// Returns <see langword="true"/> if this service implements <see cref="IMethodHandler"/>.
    /// </summary>
    public bool IsMethodHandler { get; }

    public ExportLspServiceFactoryAttribute(Type type, string contractName, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any)
        : base(contractName, typeof(ILspServiceFactory))
    {
        Contract.ThrowIfFalse(type.GetInterfaces().Contains(typeof(ILspService)), $"{type.Name} does not inherit from {nameof(ILspService)}");
        Contract.ThrowIfNull(type.AssemblyQualifiedName);

        AssemblyQualifiedName = type.AssemblyQualifiedName;
        ServerKind = serverKind;
        IsMethodHandler = typeof(IMethodHandler).IsAssignableFrom(type);
    }
}
