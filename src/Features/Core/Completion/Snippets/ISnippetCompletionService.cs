// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion.Snippets
{
    internal interface ISnippetCompletionService
    {
        /// <summary>
        /// True if typing ?[tab] should try to show the list of available snippets.
        /// </summary>
        bool SupportSnippetCompletionListOnTab { get; }
    }
}
