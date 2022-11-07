// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportWorkspaceService(typeof(IGlyphConversionService), ServiceLayer.Editor), Shared]
    internal sealed class EditorGlyphConversionService : IGlyphConversionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorGlyphConversionService()
        {
        }

        public (Guid guid, int id)? ConvertToImageId(Glyph glyph)
        {
            var imageId = glyph.GetImageId();
            return (imageId.Guid, imageId.Id);
        }
    }
}
