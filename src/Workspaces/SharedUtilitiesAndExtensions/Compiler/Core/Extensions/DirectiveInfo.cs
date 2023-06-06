// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal sealed class DirectiveInfo<TDirectiveTriviaSyntax>
        where TDirectiveTriviaSyntax : SyntaxNode
    {
        // Maps a directive to its pair
        public IDictionary<TDirectiveTriviaSyntax, TDirectiveTriviaSyntax?> DirectiveMap { get; }

        // Maps a #If/#elif/#else/#endIf directive to its list of matching #If/#elif/#else/#endIf directives
        public IDictionary<TDirectiveTriviaSyntax, ImmutableArray<TDirectiveTriviaSyntax>> ConditionalMap { get; }

        // A set of inactive regions spans.  The items in the tuple are the start and end line
        // *both inclusive* of the inactive region. Actual PP lines are not continued within.
        //
        // Note: an interval tree might be a better structure here if there are lots of inactive
        // regions.  Consider switching to that if necessary.
        public ISet<Tuple<int, int>>? InactiveRegionLines { get; }

        public DirectiveInfo(
            IDictionary<TDirectiveTriviaSyntax, TDirectiveTriviaSyntax?> directiveMap,
            IDictionary<TDirectiveTriviaSyntax, ImmutableArray<TDirectiveTriviaSyntax>> conditionalMap,
            ISet<Tuple<int, int>>? inactiveRegionLines)
        {
            this.DirectiveMap = directiveMap;
            this.ConditionalMap = conditionalMap;
            this.InactiveRegionLines = inactiveRegionLines;
        }
    }
}
