// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    [Export(typeof(IHighlightingService))]
    [Shared]
    internal class HighlightingService : IHighlightingService
    {
        private readonly List<Lazy<IHighlighter, LanguageMetadata>> _highlighters;
        private static readonly PooledObjects.ObjectPool<List<TextSpan>> s_listPool = new PooledObjects.ObjectPool<List<TextSpan>>(() => new List<TextSpan>());

        [ImportingConstructor]
        public HighlightingService(
            [ImportMany] IEnumerable<Lazy<IHighlighter, LanguageMetadata>> highlighters)
        {
            _highlighters = highlighters.ToList();
        }

        public void AddHighlights(
             SyntaxNode root, int position, List<TextSpan> highlights, CancellationToken cancellationToken)
        {
            using (s_listPool.GetPooledObject(out var tempHighlights))
            {
                foreach (var highlighter in _highlighters.Where(h => h.Metadata.Language == root.Language))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    highlighter.Value.AddHighlights(root, position, tempHighlights, cancellationToken);
                }

                tempHighlights.Sort();
                var lastSpan = default(TextSpan);
                foreach (var span in tempHighlights)
                {
                    if (span != lastSpan && !span.IsEmpty)
                    {
                        highlights.Add(span);
                        lastSpan = span;
                    }
                }
            }
        }
    }
}
