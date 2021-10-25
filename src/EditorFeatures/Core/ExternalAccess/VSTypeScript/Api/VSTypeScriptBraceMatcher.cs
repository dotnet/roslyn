// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal abstract class VSTypeScriptBraceMatcher : IBraceMatcher
    {
        Task<BraceMatchingResult?> IBraceMatcher.FindBracesAsync(Document document, int position, CancellationToken cancellationToken)
            => FindBracesAsync(document, position, cancellationToken);

        protected abstract Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
