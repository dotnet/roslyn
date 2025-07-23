// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics;

[ExportLanguageService(typeof(IAnalyzerDriverService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpAnalyzerDriverService() : AbstractAnalyzerDriverService
{
    protected override void ComputeDeclarationsInSpan(
        SemanticModel model,
        TextSpan span,
        ArrayBuilder<DeclarationInfo> builder,
        CancellationToken cancellationToken)
    {
        CSharpDeclarationComputer.ComputeDeclarationsInSpan(model, span, getSymbol: true, builder, cancellationToken);
    }
}
