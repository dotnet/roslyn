// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal interface IGlyphConversionService : IWorkspaceService
    {
        (Guid guid, int id)? ConvertToImageId(Glyph glyph);
    }

    [ExportWorkspaceService(typeof(IWorkspaceService)), Shared]
    internal sealed class DefaultGlyphConversionService : IGlyphConversionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultGlyphConversionService()
        {
        }

        public (Guid guid, int id)? ConvertToImageId(Glyph glyph) => null;
    }
}
