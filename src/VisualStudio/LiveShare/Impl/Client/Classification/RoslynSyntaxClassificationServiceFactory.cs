// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification
{
    internal abstract class RoslynSyntaxClassificationServiceFactory : ILanguageServiceFactory
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IThreadingContext _threadingContext;

        public RoslynSyntaxClassificationServiceFactory(AbstractLspClientServiceFactory roslynLspClientServiceFactory,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
            _classificationTypeMap = classificationTypeMap ?? throw new ArgumentNullException(nameof(classificationTypeMap));
            _threadingContext = threadingContext ?? throw new ArgumentNullException(nameof(threadingContext));
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new RoslynSyntaxClassificationService(_roslynLspClientServiceFactory,
                languageServices.GetOriginalLanguageService<ISyntaxClassificationService>(), _classificationTypeMap, _threadingContext);
        }
    }
}
