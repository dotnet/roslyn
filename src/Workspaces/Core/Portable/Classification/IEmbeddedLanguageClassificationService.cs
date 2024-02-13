// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IEmbeddedLanguageClassificationService : ILanguageService
    {
        Task AddEmbeddedLanguageClassificationsAsync(
            Document document,
            ImmutableArray<TextSpan> textSpans,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        void AddEmbeddedLanguageClassifications(
            SolutionServices solutionServices,
            Project project,
            SemanticModel semanticModel,
            ImmutableArray<TextSpan> textSpans,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken);
    }
}
