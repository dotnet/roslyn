// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class CSharpEmbeddedLanguageCompletionProvider : AbstractEmbeddedLanguageCompletionProvider
    {
        public CSharpEmbeddedLanguageCompletionProvider() 
            : base(CSharpEmbeddedLanguagesProvider.Instance)
        {
        }
    }
}
