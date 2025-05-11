// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics;

[ExportLanguageService(typeof(IAnalyzerDriverService), LanguageNames.CSharp), Shared]
internal sealed class CSharpAnalyzerDriverService : IAnalyzerDriverService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpAnalyzerDriverService()
    {
    }

    public void ComputeDeclarationsInSpan(
        SemanticModel model,
        TextSpan span,
        bool getSymbol,
        ArrayBuilder<DeclarationInfo> builder,
        CancellationToken cancellationToken)
    {
        CSharpDeclarationComputer.ComputeDeclarationsInSpan(model, span, getSymbol, builder, cancellationToken);
    }
}
