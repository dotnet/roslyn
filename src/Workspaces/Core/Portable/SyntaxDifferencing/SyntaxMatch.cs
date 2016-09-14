// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.TreeDifferencing;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    public struct SyntaxMatch
    {
        private readonly Match<SyntaxNode> _match;

        internal SyntaxMatch(Match<SyntaxNode> match)
        {
            _match = match;
        }

        public SyntaxNode OldNode => _match.OldRoot;
        public SyntaxNode NewNode => _match.NewRoot;

        /// <summary>
        /// Returns an edit script (a sequence of edits) that transform <see cref="OldNode"/>
        /// into <see cref="NewNode"/>.
        /// </summary>
        public ImmutableArray<SyntaxEdit> GetEdits() 
            => _match.GetTreeEdits().Edits.SelectAsArray(e => new SyntaxEdit(e));
    }
}