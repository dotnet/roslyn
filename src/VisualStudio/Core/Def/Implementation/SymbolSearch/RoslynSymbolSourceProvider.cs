// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    [Export(typeof(ISymbolSourceProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(RoslynSymbolSourceProvider))]
    internal class RoslynSymbolSourceProvider : ISymbolSourceProvider
    {
        [Import]
        internal IPersistentSpanFactory PersistentSpanFactory { get; private set; }

        [Import]
        internal ISymbolSearchBroker SymbolSearchBroker { get; private set; }

        /// <summary>
        /// <see cref="GetOrCreate(ITextBuffer)"/> is capable of returning multiple <see cref="ISymbolSource"/>s,
        /// and each source can return <see cref="SymbolSearchResult"/>s with varying <see cref="ISymbolOrigin"/>s.
        /// Currently, we are using only <see cref="RoslynSymbolSource"/> 
        /// and will cache the return value of <see cref="GetOrCreate(ITextBuffer)"/> in this variable.
        /// </summary>
        private ImmutableArray<ISymbolSource> _cachedSources;

        private object _cacheLock = new object();

        public ImmutableArray<ISymbolSource> GetOrCreate(ITextBuffer buffer)
        {
            lock (_cacheLock)
            {
                if (_cachedSources == default)
                {
                    _cachedSources = ImmutableArray.Create<ISymbolSource>(new RoslynSymbolSource(this));
                }
            }

            return _cachedSources;
        }
    }
}
