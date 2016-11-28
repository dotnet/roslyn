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
    internal abstract class AbstractOutliningService : ISynchronousOutliningService
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

        /// <summary>
        /// Keep in sync with <see cref="GetOutliningSpansAsync"/>
        /// </summary>
        public IList<OutliningSpan> GetOutliningSpans(
            Document document, CancellationToken cancellationToken)
        {
            try
            {
                var syntaxRoot = document.GetSyntaxRootSynchronously(cancellationToken);

                // change this to shared pool once RI
                var regions = new List<OutliningSpan>();
                RegionCollector.CollectOutliningSpans(document, syntaxRoot, _nodeOutlinerMap, _triviaOutlinerMap, regions, cancellationToken);
                return regions;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Keep in sync with <see cref="GetOutliningSpans"/>
        /// </summary>
        public async Task<IList<OutliningSpan>> GetOutliningSpansAsync(
            Document document, CancellationToken cancellationToken)
        {
            try
            {
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // change this to shared pool once RI
                var regions = new List<OutliningSpan>();
                RegionCollector.CollectOutliningSpans(document, syntaxRoot, _nodeOutlinerMap, _triviaOutlinerMap, regions, cancellationToken);
                return regions;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
