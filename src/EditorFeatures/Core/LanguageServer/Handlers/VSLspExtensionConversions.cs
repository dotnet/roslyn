// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class VSLspExtensionConversions
    {
        public static LSP.VSImageId GetImageIdFromGlyph(Glyph glyph)
        {
            var imageId = glyph.GetImageId();
            return new LSP.VSImageId
            {
                Guid = imageId.Guid,
                Id = imageId.Id
            };
        }
    }
}
