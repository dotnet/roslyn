// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    [ExportBraceMatcher(LanguageNames.FSharp)]
    internal class FSharpBraceMatcher : IBraceMatcher
    {
        private readonly IFSharpBraceMatcher _braceMatcher;

        [ImportingConstructor]
        public FSharpBraceMatcher(IFSharpBraceMatcher braceMatcher)
        {
            _braceMatcher = braceMatcher;
        }

        public async Task<Microsoft.CodeAnalysis.Editor.BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken = default)
        {
            var result = await _braceMatcher.FindBracesAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (result.HasValue)
            {
                return new Microsoft.CodeAnalysis.Editor.BraceMatchingResult(result.Value.LeftSpan, result.Value.RightSpan);
            }
            else
            {
                return null;
            }
        }
    }
}
