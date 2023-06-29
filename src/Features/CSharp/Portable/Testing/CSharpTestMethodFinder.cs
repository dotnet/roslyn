// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Features.Testing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Testing;

[ExportLanguageService(typeof(ITestMethodFinder), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CSharpTestMethodFinder([ImportMany] IEnumerable<ITestFrameworkMetadata> testFrameworks) : AbstractTestMethodFinder<MethodDeclarationSyntax>(testFrameworks)
{
    protected override async Task<bool> IsTestMethodAsync(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
    {
        var attributes = method.AttributeLists.SelectMany(a => a.Attributes);
        foreach (var attribute in attributes)
        {
            var isTestAttribute = await IsTestAttributeAsync(document, attribute, cancellationToken).ConfigureAwait(false);
            if (isTestAttribute)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsTestAttributeAsync(Document document, AttributeSyntax attribute, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbol = semanticModel.GetSymbolInfo(attribute, cancellationToken);
        if (symbol.Symbol is null)
        {
            return false;
        }

        var typeName = symbol.Symbol.ContainingType.ToNameDisplayString();
        var matches = TestFrameworkMetadata.Any(metadata => metadata.MatchesAttributeSymbolName(typeName));
        return matches;
    }
}
