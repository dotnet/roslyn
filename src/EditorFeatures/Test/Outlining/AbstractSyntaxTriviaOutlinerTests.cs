﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
{
    public abstract class AbstractSyntaxTriviaOutlinerTests : AbstractSyntaxOutlinerTests
    {
        internal abstract AbstractSyntaxOutliner CreateOutliner();

        internal sealed override async Task<OutliningSpan[]> GetRegionsAsync(Document document, int position)
        {
            var root = await document.GetSyntaxRootAsync(CancellationToken.None);
            var trivia = root.FindTrivia(position, findInsideTrivia: true);

            var outliner = CreateOutliner();
            var actualRegions = new List<OutliningSpan>();
            outliner.CollectOutliningSpans(document, trivia, actualRegions, CancellationToken.None);

            // TODO: Determine why we get null outlining spans.
            return actualRegions.WhereNotNull().ToArray();
        }
    }
}
