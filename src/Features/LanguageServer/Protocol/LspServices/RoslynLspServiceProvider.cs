// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export(typeof(CSharpVisualBasicLspServiceProvider)), Shared]
internal class CSharpVisualBasicLspServiceProvider : AbstractLspServiceProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpVisualBasicLspServiceProvider(
        [ImportMany(ProtocolConstants.RoslynLspLanguagesContract)] IEnumerable<Lazy<ILspService, LspServiceMetadataView>> lspServices,
        [ImportMany(ProtocolConstants.RoslynLspLanguagesContract)] IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> lspServiceFactories,
        [ImportMany(ProtocolConstants.GeneralLspLanguagesContract)] IEnumerable<Lazy<ILspService, LspServiceMetadataView>> generalLspServices,
        [ImportMany(ProtocolConstants.GeneralLspLanguagesContract)] IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> generalLspServiceFactories)
    : base(lspServices, lspServiceFactories, generalLspServices, generalLspServiceFactories)
    {
    }
}
