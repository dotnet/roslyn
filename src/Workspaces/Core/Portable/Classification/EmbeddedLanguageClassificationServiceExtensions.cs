// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class EmbeddedLanguageClassificationServiceExtensions
    {
        public static void AddEmbeddedLanguageClassifications(
            this IEmbeddedLanguageClassificationService classificationService,
            SolutionServices solutionServices,
            Project project,
            SemanticModel semanticModel,
            TextSpan textSpan,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            classificationService.AddEmbeddedLanguageClassifications(
                solutionServices,
                project,
                semanticModel,
                ImmutableArray.Create(textSpan),
                options,
                result,
                cancellationToken);
        }
    }
}
