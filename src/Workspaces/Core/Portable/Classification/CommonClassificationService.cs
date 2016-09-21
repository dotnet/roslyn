// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// The common classification service base class used by C# and VB
    /// </summary>
    internal abstract partial class CommonClassificationService : ClassificationServiceWithProviders
    {
        protected CommonClassificationService(Workspace workspace)
            : base(workspace)
        {
        }

        public ImmutableArray<ClassifiedSpan> GetSyntacticClassifications(SyntaxTree tree, TextSpan span, Workspace workspace, CancellationToken cancellationToken)
        {
            var spans = SharedPools.Default<List<ClassifiedSpan>>().Allocate();
            var spanSet = SharedPools.Default<HashSet<ClassifiedSpan>>().Allocate();
            try
            {
                var context = new ClassificationContext(spans, spanSet);
                foreach (var provider in GetProviders())
                {
                    var commonProvider = provider as CommonClassificationProvider;
                    if (commonProvider != null)
                    {
                        commonProvider.AddSyntacticClassifications(tree, span, workspace, context, cancellationToken);
                    }
                }

                return spans.ToImmutableArray();
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(spans);
                SharedPools.Default<HashSet<ClassifiedSpan>>().ClearAndFree(spanSet);
            }
        }

        public ImmutableArray<ClassifiedSpan> GetSemanticClassifications(SemanticModel model, TextSpan span, Workspace workspace, CancellationToken cancellationToken)
        {
            var spans = SharedPools.Default<List<ClassifiedSpan>>().Allocate();
            var spanSet = SharedPools.Default<HashSet<ClassifiedSpan>>().Allocate();
            try
            {
                var context = new ClassificationContext(spans, spanSet);
                foreach (var provider in GetProviders())
                {
                    var commonProvider = provider as CommonClassificationProvider;
                    if (commonProvider != null)
                    {
                        commonProvider.AddSemanticClassifications(model, span, workspace, context, cancellationToken);
                    }
                }

                return spans.ToImmutableArray();
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(spans);
                SharedPools.Default<HashSet<ClassifiedSpan>>().ClearAndFree(spanSet);
            }
        }
    }
}
