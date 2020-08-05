// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    [Export(typeof(CodeActionsCache)), Shared]
    internal class CodeActionsCache
    {
        public Document? Document { get; private set; }

        public LSP.Range? Range { get; private set; }

        public ImmutableArray<UnifiedSuggestedActionSet> CachedSuggestedActionSets { get; private set; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsCache()
        {
        }

        public void UpdateCache(
            Document document,
            LSP.Range range,
            ImmutableArray<UnifiedSuggestedActionSet> cachedSuggestedActionSets)
        {
            Document = document;
            Range = range;
            CachedSuggestedActionSets = cachedSuggestedActionSets;
        }

        public bool TryGetCache(
            Document document,
            LSP.Range range,
            [NotNullWhen(true)] out ImmutableArray<UnifiedSuggestedActionSet>? cachedSuggestedActionSets)
        {
            if (document != Document || document.Project.Solution != Document.Project.Solution ||
                range.Start != Range?.Start || range.End != Range?.End)
            {
                cachedSuggestedActionSets = null;
                return false;
            }

            cachedSuggestedActionSets = CachedSuggestedActionSets;
            return true;
        }
    }
}
