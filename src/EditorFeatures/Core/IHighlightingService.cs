// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IHighlightingService
    {
        /// <summary>
        /// Adds all the relevant highlihts to <paramref name="highlights"/> given the specified
        /// <paramref name="position"/> in the tree.
        /// <para/>
        /// Highlights will be unique and will be in sorted order. All highlights will be non-empty.
        /// </summary>
        void AddHighlights(SyntaxNode root, int position, List<TextSpan> highlights, CancellationToken cancellationToken);
    }
}
