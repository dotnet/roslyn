// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    [Export(typeof(IHighlightingService))]
    [Shared]
    internal class HighlightingService : IHighlightingService
    {
        private readonly List<Lazy<IHighlighter, LanguageMetadata>> _highlighters;

        [ImportingConstructor]
        public HighlightingService(
            [ImportMany] IEnumerable<Lazy<IHighlighter, LanguageMetadata>> highlighters)
        {
            _highlighters = highlighters.ToList();
        }

        public IEnumerable<TextSpan> GetHighlights(
             SyntaxNode root, int position, CancellationToken cancellationToken)
        {
            return _highlighters.Where(h => h.Metadata.Language == root.Language)
                                .Select(h => h.Value.GetHighlights(root, position, cancellationToken))
                                .WhereNotNull()
                                .Flatten()
                                .Distinct();
        }
    }
}
