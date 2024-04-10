// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

[ExportLanguageService(typeof(ISyntaxContextService), LanguageNames.CSharp), Shared]
internal class CSharpSyntaxContextService : ISyntaxContextService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSyntaxContextService()
    {
    }

    public SyntaxContext CreateContext(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        => CSharpSyntaxContext.CreateContext(document, semanticModel, position, cancellationToken);
}
