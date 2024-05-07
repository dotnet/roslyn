// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

/// <summary>
/// Base class for services that need to live in Razor but be exported using <see cref="ExportRazorStatelessLspServiceAttribute"/>
/// since those services must implement <see cref="ILspService"/> but the Razor code doesn't have IVT to it.
/// </summary>
internal abstract class AbstractRazorLspService : ILspService
{
}
