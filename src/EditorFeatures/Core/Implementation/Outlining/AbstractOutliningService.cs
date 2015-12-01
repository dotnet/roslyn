// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal abstract class AbstractOutliningService : IOutliningService
    {
        private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>> _nodeOutlinerMap;
        private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxOutliner>> _triviaOutlinerMap;

        protected AbstractOutliningService(
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>> defaultNodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxOutliner>> defaultTriviaOutlinerMap)
        {
            _nodeOutlinerMap = defaultNodeOutlinerMap;
            _triviaOutlinerMap = defaultTriviaOutlinerMap;
        }

        public async Task<IList<OutliningSpan>> GetOutliningSpansAsync(Document document, CancellationToken cancellationToken)
        {
            try
            {
                var syntaxDocument = await SyntacticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                // change this to shared pool once RI
                var regions = new List<OutliningSpan>();
                RegionCollector.CollectOutliningSpans(syntaxDocument, _nodeOutlinerMap, _triviaOutlinerMap, regions, cancellationToken);

                return regions;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
