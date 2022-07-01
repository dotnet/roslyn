// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig;

[Export(typeof(EditorConfigLspServiceProvider)), Shared]
internal class EditorConfigLspServiceProvider : AbstractLspServiceProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EditorConfigLspServiceProvider(
        [ImportMany(ProtocolConstants.EditorConfigLanguageContract)] IEnumerable<Lazy<ILspService, LspServiceMetadataView>> lspServices,
        [ImportMany(ProtocolConstants.EditorConfigLanguageContract)] IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> lspServiceFactories) : base(lspServices, lspServiceFactories)
    {
    }
}
