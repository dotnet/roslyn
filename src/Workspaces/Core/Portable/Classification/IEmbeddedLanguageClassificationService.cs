// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IEmbeddedLanguageClassificationService : ILanguageService
    {
        Task AddEmbeddedLanguageClassificationsAsync(
            Document document,
            TextSpan textSpan,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        void AddEmbeddedLanguageClassifications(
            SolutionServices solutionServices,
            Project project,
            SemanticModel semanticModel,
            TextSpan textSpan,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken);
    }
}
