// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    [Export(typeof(IBraceMatchingService))]
    internal class BraceMatchingService : IBraceMatchingService
    {
        private readonly List<Lazy<IBraceMatcher, LanguageMetadata>> _braceMatchers;

        [ImportingConstructor]
        public BraceMatchingService(
            [ImportMany] IEnumerable<Lazy<IBraceMatcher, LanguageMetadata>> braceMatchers)
        {
            ////braceMatchers.RealizeImports();
            _braceMatchers = braceMatchers.ToList();
        }

        public async Task<BraceMatchingResult?> GetMatchingBracesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (position < 0 || position > text.Length)
            {
                throw new ArgumentException(nameof(position));
            }

            var matchers = _braceMatchers.Where(b => b.Metadata.Language == document.Project.Language);
            foreach (var matcher in matchers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var braces = await matcher.Value.FindBracesAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (braces.HasValue)
                {
                    return braces;
                }
            }

            return null;
        }
    }
}
