// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptInlineRenameReplacementWrapper(InlineRenameReplacement underlyingObject)
    {
        internal readonly InlineRenameReplacement UnderlyingObject = underlyingObject;

        public VSTypeScriptInlineRenameReplacementKind Kind => VSTypeScriptInlineRenameReplacementKindHelpers.ConvertFrom(UnderlyingObject.Kind);
        public TextSpan OriginalSpan => UnderlyingObject.OriginalSpan;
        public TextSpan NewSpan => UnderlyingObject.NewSpan;
    }
}
