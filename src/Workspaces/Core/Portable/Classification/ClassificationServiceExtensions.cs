// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal static partial class ClassificationServiceExtensions
{
    public static void AddSyntacticClassifications(
        this IClassificationService classificationService,
        SolutionServices services,
        SyntaxNode? root,
        TextSpan textSpan,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        classificationService.AddSyntacticClassifications(services, root, [textSpan], result, cancellationToken);
    }

    public static Task AddSyntacticClassificationsAsync(
        this IClassificationService classificationService,
        Document document,
        TextSpan textSpan,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        return classificationService.AddSyntacticClassificationsAsync(document, [textSpan], result, cancellationToken);
    }

    public static Task AddSemanticClassificationsAsync(
        this IClassificationService classificationService,
        Document document,
        TextSpan textSpan,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        return classificationService.AddSemanticClassificationsAsync(document, [textSpan], options, result, cancellationToken);
    }

    public static Task AddEmbeddedLanguageClassificationsAsync(
        this IClassificationService classificationService,
        Document document,
        TextSpan textSpan,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        return classificationService.AddEmbeddedLanguageClassificationsAsync(document, [textSpan], options, result, cancellationToken);
    }
}
