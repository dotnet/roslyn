// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal interface IEmbeddedLanguageProvider : ILanguageService
    {
        ImmutableArray<IEmbeddedLanguage> GetEmbeddedLanguages();
    }
}
