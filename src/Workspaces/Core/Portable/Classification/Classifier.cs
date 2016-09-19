// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// The <see cref="Classifier"/> provides language classifications for ranges of text.
    /// </summary>
    public static class Classifier
    {
        /// <summary>
        /// Gets language classifications for a range of the document corresponding to the specified text span.
        /// </summary>
        public static async Task<IEnumerable<ClassifiedSpan>> GetClassifiedSpansAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var service = ClassificationService.GetService(document);
            if (service != null)
            {
                var syntacticClassifications = await service.GetSyntacticClassificationsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                var semanticClassifications = await service.GetSemanticClassificationsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                return Merge(textSpan, syntacticClassifications, semanticClassifications);
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<ClassifiedSpan>();
            }
        }

        /// <summary>
        /// Gets language classifications for a range of the document corresponding to the specified text span,
        /// given a specific semantic model.
        /// </summary>
        public static IEnumerable<ClassifiedSpan> GetClassifiedSpans(
            SemanticModel semanticModel,
            TextSpan textSpan,
            Workspace workspace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var service = ClassificationService.GetService(workspace, semanticModel.Language) as CommonClassificationService;
            if (service != null)
            {
                var syntacticClassifications = service.GetSyntacticClassifications(semanticModel.SyntaxTree, textSpan, workspace, cancellationToken);
                var semanticClassifications = service.GetSemanticClassifications(semanticModel, textSpan, workspace, cancellationToken);
                return Merge(textSpan, syntacticClassifications, semanticClassifications);
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<ClassifiedSpan>();
            }
        }

        private static ImmutableArray<ClassifiedSpan> Merge(TextSpan textSpan, ImmutableArray<ClassifiedSpan> syntacticClassifications, ImmutableArray<ClassifiedSpan> semanticClassifications)
        {
            var list = SharedPools.Default<List<ClassifiedSpan>>().Allocate();
            try
            {
                list.AddRange(semanticClassifications.Where(cs => cs.TextSpan.OverlapsWith(textSpan)));
                var semanticSet = semanticClassifications.Select(cs => cs.TextSpan).ToImmutableHashSet();
                list.AddRange(syntacticClassifications.Where(cs => cs.TextSpan.OverlapsWith(textSpan) && !semanticSet.Contains(cs.TextSpan)));
                list.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);
                return list.ToImmutableArray();
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(list);
            }
        }
    }
}