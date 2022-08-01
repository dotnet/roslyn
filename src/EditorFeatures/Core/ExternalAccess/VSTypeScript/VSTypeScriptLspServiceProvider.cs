// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Export(typeof(VSTypeScriptLspServiceProvider)), Shared]
internal class VSTypeScriptLspServiceProvider : AbstractLspServiceProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptLspServiceProvider(
        [ImportMany(ProtocolConstants.TypeScriptLanguageContract)] IEnumerable<Lazy<ILspService, LspServiceMetadataView>> lspServices,
        [ImportMany(ProtocolConstants.TypeScriptLanguageContract)] IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> lspServiceFactories,
        [ImportMany(ProtocolConstants.GeneralLspLanguagesContract)] IEnumerable<Lazy<ILspService, LspServiceMetadataView>> generalLspServices,
        [ImportMany(ProtocolConstants.GeneralLspLanguagesContract)] IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> generalLspServiceFactories)
    : base(lspServices, lspServiceFactories, generalLspServices, generalLspServiceFactories)
    {
    }
}
