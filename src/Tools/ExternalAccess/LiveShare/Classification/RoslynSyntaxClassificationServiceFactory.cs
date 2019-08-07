// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Classification
{
    internal abstract class RoslynSyntaxClassificationServiceFactory : ILanguageServiceFactory
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly RemoteLanguageServiceWorkspace _remoteLanguageServiceWorkspace;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IThreadingContext _threadingContext;

        public RoslynSyntaxClassificationServiceFactory(AbstractLspClientServiceFactory roslynLspClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
            _remoteLanguageServiceWorkspace = remoteLanguageServiceWorkspace ?? throw new ArgumentNullException(nameof(remoteLanguageServiceWorkspace));
            _classificationTypeMap = classificationTypeMap ?? throw new ArgumentNullException(nameof(classificationTypeMap));
            _threadingContext = threadingContext ?? throw new ArgumentNullException(nameof(threadingContext));
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            var experimentationService = languageServices.WorkspaceServices.GetService<IExperimentationService>();

            return new RoslynSyntaxClassificationService(_roslynLspClientServiceFactory, _remoteLanguageServiceWorkspace,
                languageServices.GetOriginalLanguageService<ISyntaxClassificationService>(), _classificationTypeMap, experimentationService, _threadingContext);
        }
    }
}
