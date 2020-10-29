// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Shared.Extensions
{
    internal static class DocumentExtensions
    {
        public static bool CanAddUsingDirectives(this Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
#if !CODE_STYLE
            // Normally we don't allow generation into a hidden region in the file.  However, if we have a
            // modern span mapper at our disposal, we do allow it as that host span mapper can handle mapping
            // our edit to their domain appropriate.
            var spanMapper = document.Services.GetService<ISpanMappingService>();
            var allowInHiddenRegions = spanMapper != null && !spanMapper.IsLegacy;
#else
            var allowInHiddenRegions = false;
#endif
            return node.CanAddUsingDirectives(allowInHiddenRegions, cancellationToken);
        }
    }
}
