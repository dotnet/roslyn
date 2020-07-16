// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [ExportBraceMatcher(LanguageNames.FSharp)]
    internal class FSharpBraceMatcher : IBraceMatcher
    {
        private readonly IFSharpBraceMatcher _braceMatcher;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
