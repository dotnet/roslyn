// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageProvider : IEmbeddedLanguageProvider
    {
        private readonly ImmutableArray<IEmbeddedLanguage> _embeddedLanguages;

        protected AbstractEmbeddedLanguageProvider(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            // This is where we'll add the Regex and Json providers when their respective
            // branches get merged in.
            _embeddedLanguages = ImmutableArray.Create<IEmbeddedLanguage>();
        }

        public ImmutableArray<IEmbeddedLanguage> GetEmbeddedLanguages()
            => _embeddedLanguages;

        /// <summary>
        /// Helper method used by the VB and C# IEmbeddedCodeFixProviders so they can
        /// add special comments to string literals to convey that language services should light up
        /// for them.
        /// </summary>
        internal abstract void AddComment(
            SyntaxEditor editor, SyntaxToken stringLiteral, string commentContents);
    }
}
