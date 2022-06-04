// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    [ExportContentTypeLanguageService(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName, InteractiveLanguageNames.InteractiveCommand), Shared]
    internal class InteractiveCommandContentTypeLanguageService : IContentTypeLanguageService
    {
        private readonly IContentTypeRegistryService _contentTypeRegistry;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractiveCommandContentTypeLanguageService(IContentTypeRegistryService contentTypeRegistry)
            => _contentTypeRegistry = contentTypeRegistry;

        public IContentType GetDefaultContentType()
            => _contentTypeRegistry.GetContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName);
    }
}
