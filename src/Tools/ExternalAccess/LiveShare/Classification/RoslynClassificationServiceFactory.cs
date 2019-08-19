// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Classification
{

    internal abstract class RoslynClassificationServiceFactory : ILanguageServiceFactory
    {
        protected abstract string LiveShareContentType { get; }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            // Get the liveshare language service for ISyntaxClassificationService.
            var liveshareSyntaxClassificationService = languageServices.WorkspaceServices.GetLanguageServices(LiveShareContentType).GetService<ISyntaxClassificationService>();
            // Get the original language service for IClassificationService.
            var originalService = languageServices.GetOriginalLanguageService<IClassificationService>();
            return new RoslynClassificationService(originalService, liveshareSyntaxClassificationService);
        }
    }
}
