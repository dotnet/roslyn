// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Highlighting
{
    [Export(typeof(IHighlightingService))]
    [Shared]
    internal class HighlightingService : IHighlightingService
    {
        private readonly List<Lazy<IHighlighter, LanguageMetadata>> _highlighters;
        private static readonly PooledObjects.ObjectPool<List<TextSpan>> s_listPool = new(() => new List<TextSpan>());

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
