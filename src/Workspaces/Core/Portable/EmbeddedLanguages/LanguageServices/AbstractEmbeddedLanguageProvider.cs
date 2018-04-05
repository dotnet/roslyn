// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal abstract class AbstractEmbeddedLanguageProvider : IEmbeddedLanguageProvider
    {
        private readonly ImmutableArray<IEmbeddedLanguage> _embeddedLanguages;

        protected AbstractEmbeddedLanguageProvider(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            _embeddedLanguages = ImmutableArray.Create<IEmbeddedLanguage>(
                new JsonEmbeddedLanguage(stringLiteralKind, syntaxFacts, semanticFacts, virtualCharService));
        }

        public ImmutableArray<IEmbeddedLanguage> GetEmbeddedLanguages()
            => _embeddedLanguages;
    }

    //[ExportWorkspaceService(typeof(IEmbeddedLanguageProvider)), Shared]
    //internal class DefaultEmbeddedLanguageProvider : IEmbeddedLanguageProvider
    //{
    //    private readonly ImmutableArray<IEmbeddedLanguage> _embeddedLanguages;

    //    [ImportingConstructor]
    //    public DefaultEmbeddedLanguageProvider(IEnumerable<IEmbeddedLanguage> embeddedLanguages)
    //    {
    //        _embeddedLanguages = ImmutableArray.CreateRange(embeddedLanguages);
    //    }

    //    public ImmutableArray<IEmbeddedLanguage> GetEmbeddedLanguages()
    //        => _embeddedLanguages;
    //}
}
