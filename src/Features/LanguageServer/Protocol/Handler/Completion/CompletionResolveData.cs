// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class CompletionResolveData
    {
        public TextDocumentIdentifier TextDocument { get; set; }

        public Position Position { get; set; }

        public string DisplayText { get; set; }
    }
}
