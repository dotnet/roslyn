// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Completion.FileSystem
{
    internal sealed class GlobalAssemblyCacheCompletionHelper
    {
        private static readonly Lazy<List<string>> s_lazyAssemblySimpleNames =
            new Lazy<List<string>>(() => GlobalAssemblyCache.Instance.GetAssemblySimpleNames().ToList());

        private readonly CompletionProvider _completionProvider;
        private readonly TextSpan _textChangeSpan;
        private readonly CompletionItemRules _itemRules;

        public GlobalAssemblyCacheCompletionHelper(
            CompletionProvider completionProvider, 
            TextSpan textChangeSpan, 
            CompletionItemRules itemRules = null)
        {
            _completionProvider = completionProvider;
            _textChangeSpan = textChangeSpan;
            _itemRules = itemRules;
        }

        internal Task<ImmutableArray<CompletionItem>> GetItemsAsync(string pathSoFar, CancellationToken cancellationToken)
        {
            return Task.Run(() => GetCompletionsWorker(pathSoFar).ToImmutableArray(), cancellationToken);
        }

        private IEnumerable<CompletionItem> GetCompletionsWorker(string pathSoFar)
        {
            var comma = pathSoFar.IndexOf(',');
            if (comma >= 0)
            {
                var path = pathSoFar.Substring(0, comma);
                return from identity in GetAssemblyIdentities(path)
                       let text = identity.GetDisplayName()
                       select CommonCompletionItem.Create(text, glyph: Glyph.Assembly, rules: _itemRules);
            }
            else
            {
                return from displayName in s_lazyAssemblySimpleNames.Value
                       select CommonCompletionItem.Create(
                           displayName,
                           description: GlobalAssemblyCache.Instance.ResolvePartialName(displayName).GetDisplayName().ToSymbolDisplayParts(),
                           glyph: Glyph.Assembly,
                           rules: _itemRules);
            }
        }

        private IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string pathSoFar)
        {
            return IOUtilities.PerformIO(() => GlobalAssemblyCache.Instance.GetAssemblyIdentities(pathSoFar),
                SpecializedCollections.EmptyEnumerable<AssemblyIdentity>());
        }
    }
}
