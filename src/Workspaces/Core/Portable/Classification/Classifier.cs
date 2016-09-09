// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    public static class Classifier
    {
        public static async Task<IEnumerable<ClassifiedSpan>> GetClassifiedSpansAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var workspace = document.Project.Solution.Workspace;
            var service = workspace.Services.GetLanguageServices(document.Project.Language).GetService<ClassificationService>();
            if (service != null)
            {
                var syntacticClassifications = await service.GetSyntacticClassificationsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                var semanticClassifications = await service.GetSemanticClassificationsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

                // merge semantic and syntatic spans, giving precedence to semantic spans
                var list = new List<ClassifiedSpan>();
                list.AddRange(semanticClassifications);
                var semanticSet = semanticClassifications.Select(c => c.TextSpan).ToImmutableHashSet();
                list.AddRange(syntacticClassifications.Where(s => !semanticSet.Contains(s.TextSpan)));
                list.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);

                return list.ToImmutableArray();
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<ClassifiedSpan>();
            }
        }

        public static IEnumerable<ClassifiedSpan> GetClassifiedSpans(
            SemanticModel semanticModel,
            TextSpan textSpan,
            Workspace workspace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var service = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ClassificationService>() as CommonClassificationService;
            if (service != null)
            {
                var syntacticClassifications = service.GetSyntacticClassifications(semanticModel.SyntaxTree, textSpan, workspace, cancellationToken);
                var semanticClassifications = service.GetSemanticClassifications(semanticModel, textSpan, workspace, cancellationToken);

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
            else
            {
                return SpecializedCollections.EmptyEnumerable<ClassifiedSpan>();
            }
        }
    }
}