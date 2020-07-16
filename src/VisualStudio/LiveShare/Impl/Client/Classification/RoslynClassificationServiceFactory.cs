// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification
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
