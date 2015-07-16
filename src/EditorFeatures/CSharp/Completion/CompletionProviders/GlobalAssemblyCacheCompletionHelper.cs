// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders
{
    internal sealed class GlobalAssemblyCacheCompletionHelper
    {
        private static readonly Lazy<List<string>> s_lazyAssemblySimpleNames =
            new Lazy<List<string>>(() => GlobalAssemblyCache.GetAssemblySimpleNames().ToList());
        private readonly CompletionListProvider _completionProvider;
        private readonly TextSpan _textChangeSpan;

        public GlobalAssemblyCacheCompletionHelper(CompletionListProvider completionProvider, TextSpan textChangeSpan)
        {
            _completionProvider = completionProvider;
            _textChangeSpan = textChangeSpan;
        }

        public IEnumerable<CompletionItem> GetItems(string pathSoFar, string documentPath)
        {
            var containsSlash = pathSoFar.Contains(@"/") || pathSoFar.Contains(@"\");
            if (containsSlash)
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            return GetCompletionsWorker(pathSoFar).ToList();
        }

        private IEnumerable<CompletionItem> GetCompletionsWorker(string pathSoFar)
        {
            var comma = pathSoFar.IndexOf(',');
            if (comma >= 0)
            {
                var path = pathSoFar.Substring(0, comma);
                return from identity in GetAssemblyIdentities(path)
                       let text = identity.GetDisplayName()
                       select new CompletionItem(_completionProvider, text, _textChangeSpan, glyph: Glyph.Assembly);
            }
            else
            {
                return from displayName in s_lazyAssemblySimpleNames.Value
                       select new CompletionItem(
                           _completionProvider,
                           displayName, _textChangeSpan,
                           descriptionFactory: c => Task.FromResult(GlobalAssemblyCache.ResolvePartialName(displayName).GetDisplayName().ToSymbolDisplayParts()),
                           glyph: Glyph.Assembly);
            }
        }

        private IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string pathSoFar)
        {
            return IOUtilities.PerformIO(() => GlobalAssemblyCache.GetAssemblyIdentities(pathSoFar),
                SpecializedCollections.EmptyEnumerable<AssemblyIdentity>());
        }
    }
}
