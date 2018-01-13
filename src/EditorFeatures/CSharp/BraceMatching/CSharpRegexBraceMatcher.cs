// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class CSharpRegexBraceMatcher : IBraceMatcher
    {
        public Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken)
            => CommonRegexBraceMatcher.FindBracesAsync(document, position, cancellationToken);
    }
}
