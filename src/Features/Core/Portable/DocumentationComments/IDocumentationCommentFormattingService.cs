﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal interface IDocumentationCommentFormattingService : ILanguageService
    {
        string Format(string rawXmlText, Compilation compilation = null);
        IEnumerable<TaggedText> Format(string rawXmlText, SemanticModel semanticModel, int position, SymbolDisplayFormat format = null);
    }
}