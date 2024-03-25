// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[Export(typeof(RazorLspServiceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorLspServiceProvider(
    [ImportMany(Constants.RazorLanguageContract)] IEnumerable<Lazy<ILspService, LspServiceMetadataView>> lspServices,
    [ImportMany(Constants.RazorLanguageContract)] IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> lspServiceFactories)
    : AbstractLspServiceProvider(lspServices, lspServiceFactories)
{
}
