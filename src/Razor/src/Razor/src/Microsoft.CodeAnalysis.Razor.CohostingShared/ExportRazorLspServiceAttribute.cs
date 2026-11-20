// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Razor.CohostingShared;

/// <summary>
/// Exports a Razor cohosting LSP service. Mark it <c>[Shared]</c> for a single instance shared by all LSP servers, or
/// <c>[Shared(LspServiceComposition.SharingBoundary)]</c> for an instance per LSP server.
/// </summary>
#pragma warning disable RS0030 // Do not use banned APIs
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false), MetadataAttribute]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class ExportRazorLspServiceAttribute(Type handlerType) : ExportLspServiceAttribute(handlerType, ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any);
