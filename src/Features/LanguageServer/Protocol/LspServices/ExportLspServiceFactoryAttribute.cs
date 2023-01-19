// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
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
    /// The type of the service being exported.  Used during retrieval to find the matching service.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The LSP server for which this service applies to.  If null, this service applies to any server
    /// with the matching contract name.
    /// </summary>
    public WellKnownLspServerKinds ServerKind { get; }

    /// <summary>
    /// Services MEF exported as <see cref="ILspServiceFactory"/> are statefull as <see cref="LspServices"/>
    /// creates a new instance for each server instance.
    /// </summary>
    public bool IsStateless { get; } = false;

    public ExportLspServiceFactoryAttribute(Type type, string contractName, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any) : base(contractName, typeof(ILspServiceFactory))
    {
        Contract.ThrowIfFalse(type.GetInterfaces().Contains(typeof(ILspService)), $"{type.Name} does not inherit from {nameof(ILspService)}");
        Type = type;
        ServerKind = serverKind;
    }
}
