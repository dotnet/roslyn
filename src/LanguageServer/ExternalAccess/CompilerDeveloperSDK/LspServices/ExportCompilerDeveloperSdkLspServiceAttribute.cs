// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

/// <summary>
/// Exports a Compiler Developer SDK LSP service.  The service must also be marked <c>[Shared]</c>, either with
/// <see cref="CompilerDeveloperSdkLspServiceComposition.SharingBoundary"/> (one instance per LSP server) or without a
/// boundary (one stateless instance shared by all LSP servers).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false), MetadataAttribute]
internal sealed class ExportCompilerDeveloperSdkLspServiceAttribute(Type type) :
    ExportCSharpVisualBasicLspServiceAttribute(type, WellKnownLspServerKinds.Any);

/// <summary>
/// Composition names for CompilerDeveloperSdk LSP services.
/// </summary>
internal static class CompilerDeveloperSdkLspServiceComposition
{
    /// <summary>
    /// MEF sharing boundary of a single LSP server.  Mark a service <c>[Shared(SharingBoundary)]</c> to create an instance
    /// of it for each LSP server; the service can import the
    /// <see cref="CompilerDeveloperSdkLspServices"/> of its LSP server, along with regular MEF parts.  Mark it with a plain <c>[Shared]</c> to share a single
    /// stateless instance between all LSP servers; such a service can only import regular MEF parts.
    /// </summary>
    public const string SharingBoundary = LspServiceComposition.SharingBoundary;
}
