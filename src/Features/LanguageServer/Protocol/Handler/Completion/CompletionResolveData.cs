// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class CompletionResolveData
    {
        public CompletionParams CompletionParams { get; set; }
        public string DisplayText { get; set; }
    }
}
