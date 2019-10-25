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

        private ImmutableArray<ISymbolSource> ExportedSources;

        public ImmutableArray<ISymbolSource> GetOrCreate(ITextBuffer buffer)
        {
            if (ExportedSources == default)
            {
                ExportedSources = ImmutableArray.Create<ISymbolSource>(new RoslynSymbolSource(this));
            }

            return ExportedSources;
        }
    }
}
