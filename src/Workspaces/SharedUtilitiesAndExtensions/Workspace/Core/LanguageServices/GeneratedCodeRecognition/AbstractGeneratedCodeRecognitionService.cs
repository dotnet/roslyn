// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GeneratedCodeRecognition;

internal abstract class AbstractGeneratedCodeRecognitionService : IGeneratedCodeRecognitionService
{
#if WORKSPACE
    public bool IsGeneratedCode(Document document, CancellationToken cancellationToken)
    {
        var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
        return IsGeneratedCode(syntaxTree, document, cancellationToken);
    }
#endif

    public async Task<bool> IsGeneratedCodeAsync(Document document, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        return IsGeneratedCode(syntaxTree, document, cancellationToken);
    }

    private static bool IsGeneratedCode(SyntaxTree syntaxTree, Document document, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
        return syntaxTree.IsGeneratedCode(document.Project.AnalyzerOptions, syntaxFacts, cancellationToken);
    }
}
