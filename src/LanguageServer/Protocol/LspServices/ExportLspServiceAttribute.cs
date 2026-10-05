// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Names used to compose per-server LSP services.
/// </summary>
internal static class LspServiceComposition
{
    /// <summary>
    /// MEF sharing boundary for a single LSP server instance.  Every call to
    /// <see cref="AbstractLspServiceProvider.CreateServices"/> creates a new instance of this boundary, so parts
    /// marked <c>[Shared(LspServiceComposition.SharingBoundary)]</c> get one instance per LSP server.
    /// </summary>
    public const string SharingBoundary = "Microsoft.CodeAnalysis.LanguageServer.LspServer";

    /// <summary>
    /// MEF contract name used by all per-server LSP service exports.  The LSP contract the service belongs to is
    /// carried in metadata instead.  Only <see cref="LspServerScope"/> may import this contract; everything else must
    /// import <see cref="LspService{T}"/>, which applies LSP contract filtering and server kind overrides.
    /// </summary>
    public const string ContractName = "Microsoft.CodeAnalysis.LanguageServer.LspService";
}

/// <summary>
/// Exports an LSP service.  Every LSP server gets its own instance of the service when the part is also marked
/// <c>[Shared(LspServiceComposition.SharingBoundary)]</c>; the service can then constructor-import other LSP services (and base
/// services) with <see cref="LspService{T}"/> and regular (process-wide) MEF parts as normal.  Services that are
/// disposable are disposed when their LSP server shuts down. Services can implement <see cref="IDisposable"/> or
/// <see cref="IAsyncDisposable"/>; disposal of the sharing boundary is synchronous and blocks until cleanup completes.
/// </summary>
/// <remarks>
/// A stateless service that should be shared by every LSP server may instead be marked with a plain
/// <c>[Shared]</c>.  Such a service is owned and disposed by the MEF container, not an individual server, and must not import
/// <see cref="LspService{T}"/>.
/// <para/>
/// A service exported for a specific <see cref="WellKnownLspServerKinds"/> overrides the
/// <see cref="WellKnownLspServerKinds.Any"/> export of the same service type for that server kind.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false), MetadataAttribute]
internal class ExportLspServiceAttribute(Type serviceType, string lspContract, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any)
    : AbstractExportLspServiceAttribute(serviceType, lspContract, serverKind);

/// <summary>
/// <see cref="ExportLspServiceAttribute"/> for the <see cref="ProtocolConstants.RoslynLspLanguagesContract"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false), MetadataAttribute]
internal class ExportCSharpVisualBasicLspServiceAttribute(Type serviceType, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any)
    : ExportLspServiceAttribute(serviceType, ProtocolConstants.RoslynLspLanguagesContract, serverKind);
