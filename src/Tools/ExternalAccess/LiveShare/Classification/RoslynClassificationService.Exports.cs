// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Classification
{
    [ExportLanguageServiceFactory(typeof(IClassificationService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspClassificationServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return languageServices.GetOriginalLanguageService<IClassificationService>();
        }
    }

    [ExportLanguageServiceFactory(typeof(IClassificationService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspClassificationServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return languageServices.GetOriginalLanguageService<IClassificationService>();
        }
    }

    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspEditorClassificationFactoryService : ILanguageServiceFactory
    {
        private readonly CSharpLspClientServiceFactory _csharpLspClientServiceFactory;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public CSharpLspEditorClassificationFactoryService(CSharpLspClientServiceFactory csharpLspClientServiceFactory, ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
        {
            _csharpLspClientServiceFactory = csharpLspClientServiceFactory ?? throw new ArgumentNullException(nameof(csharpLspClientServiceFactory));
            _classificationTypeMap = classificationTypeMap ?? throw new ArgumentNullException(nameof(classificationTypeMap));
            _threadingContext = threadingContext;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new RoslynClassificationService(_csharpLspClientServiceFactory, languageServices.GetOriginalLanguageService<ISyntaxClassificationService>(), _classificationTypeMap, _threadingContext);
        }
    }

    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspEditorClassificationFactoryService : ILanguageServiceFactory
    {
        private readonly VisualBasicLspClientServiceFactory _vbLspClientServiceFactory;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public VBLspEditorClassificationFactoryService(VisualBasicLspClientServiceFactory vbLspClientServiceFactory, ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
        {
            _vbLspClientServiceFactory = vbLspClientServiceFactory ?? throw new ArgumentNullException(nameof(vbLspClientServiceFactory));
            _classificationTypeMap = classificationTypeMap ?? throw new ArgumentNullException(nameof(classificationTypeMap));
            _threadingContext = threadingContext;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new RoslynClassificationService(_vbLspClientServiceFactory, languageServices.GetOriginalLanguageService<ISyntaxClassificationService>(), _classificationTypeMap, _threadingContext);
        }
    }
}
