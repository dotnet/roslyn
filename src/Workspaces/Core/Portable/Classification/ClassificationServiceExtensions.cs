// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal static partial class ClassificationServiceExtensions
{
    extension(IClassificationService classificationService)
    {
        public void AddSyntacticClassifications(
        SolutionServices services,
        SyntaxNode? root,
        TextSpan textSpan,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
        {
            classificationService.AddSyntacticClassifications(services, root, [textSpan], result, cancellationToken);
        }

        public Task AddSyntacticClassificationsAsync(
            Document document,
            TextSpan textSpan,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            return classificationService.AddSyntacticClassificationsAsync(document, [textSpan], result, cancellationToken);
        }

        public Task AddSemanticClassificationsAsync(
            Document document,
            TextSpan textSpan,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            return classificationService.AddSemanticClassificationsAsync(document, [textSpan], options, result, cancellationToken);
        }

        public Task AddEmbeddedLanguageClassificationsAsync(
            Document document,
            TextSpan textSpan,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            return classificationService.AddEmbeddedLanguageClassificationsAsync(document, [textSpan], options, result, cancellationToken);
        }
    }
}
