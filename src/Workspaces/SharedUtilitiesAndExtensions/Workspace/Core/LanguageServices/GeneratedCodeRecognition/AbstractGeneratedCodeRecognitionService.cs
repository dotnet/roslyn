// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GeneratedCodeRecognition
{
    internal abstract class AbstractGeneratedCodeRecognitionService : IGeneratedCodeRecognitionService
    {
#if !CODE_STYLE
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
            // First check if user has configured "generated_code = true | false" in .editorconfig
            var analyzerOptions = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            var isUserConfiguredGeneratedCode = GeneratedCodeUtilities.GetIsGeneratedCodeFromOptions(analyzerOptions);
            if (isUserConfiguredGeneratedCode.HasValue)
            {
                return isUserConfiguredGeneratedCode.Value;
            }

            // Otherwise, fallback to generated code heuristic.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return GeneratedCodeUtilities.IsGeneratedCode(
                syntaxTree, t => syntaxFacts.IsRegularComment(t) || syntaxFacts.IsDocumentationComment(t), cancellationToken);
        }
    }
}
