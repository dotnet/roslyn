// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptInlineRenameReplacementWrapper
    {
        private readonly InlineRenameReplacement _underlyingObject;

        public VSTypeScriptInlineRenameReplacementWrapper(InlineRenameReplacement underlyingObject)
            => _underlyingObject = underlyingObject;

        public VSTypeScriptInlineRenameReplacementKind Kind => VSTypeScriptInlineRenameReplacementKindHelpers.ConvertFrom(_underlyingObject.Kind);
        public TextSpan OriginalSpan => _underlyingObject.OriginalSpan;
        public TextSpan NewSpan => _underlyingObject.NewSpan;
    }
}
