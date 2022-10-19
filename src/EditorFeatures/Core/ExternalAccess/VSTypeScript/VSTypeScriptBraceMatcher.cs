// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportBraceMatcher(InternalLanguageNames.TypeScript)]
    internal sealed class VSTypeScriptBraceMatcher : IBraceMatcher
    {
        private readonly IVSTypeScriptBraceMatcherImplementation _impl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptBraceMatcher(IVSTypeScriptBraceMatcherImplementation impl)
        {
            _impl = impl;
        }

        public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            var result = await _impl.FindBracesAsync(document, position, cancellationToken).ConfigureAwait(false);
            return result.HasValue ? new BraceMatchingResult(result.Value.LeftSpan, result.Value.RightSpan) : null;
        }
    }
}
