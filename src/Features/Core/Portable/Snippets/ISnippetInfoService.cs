// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal interface ISnippetInfoService : ILanguageService
    {
        IEnumerable<SnippetInfo> GetSnippetsIfAvailable();
        bool SnippetShortcutExists_NonBlocking(string shortcut);
        bool ShouldFormatSnippet(SnippetInfo snippetInfo);
    }
}
