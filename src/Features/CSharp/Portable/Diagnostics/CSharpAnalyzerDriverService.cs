// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics
{
    [ExportLanguageService(typeof(IAnalyzerDriverService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpAnalyzerDriverService : IAnalyzerDriverService
    {
        public void ComputeDeclarationsInSpan(SemanticModel model, TextSpan span, bool getSymbol, List<DeclarationInfo> builder, CancellationToken cancellationToken)
        {
            CSharpDeclarationComputer.ComputeDeclarationsInSpan(model, span, getSymbol, builder, cancellationToken);
        }
    }
}
