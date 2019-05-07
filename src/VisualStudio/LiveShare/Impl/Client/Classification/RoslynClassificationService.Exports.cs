//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
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
        private readonly RoslynLSPClientServiceFactory roslynLSPClientServiceFactory;
        private readonly ClassificationTypeMap classificationTypeMap;

        [ImportingConstructor]
        public CSharpLspEditorClassificationFactoryService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, ClassificationTypeMap classificationTypeMap)
        {
            this.roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
            this.classificationTypeMap = classificationTypeMap ?? throw new ArgumentNullException(nameof(classificationTypeMap));
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new RoslynClassificationService(this.roslynLSPClientServiceFactory, languageServices.GetOriginalLanguageService<ISyntaxClassificationService>(), this.classificationTypeMap);
        }
    }

    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspEditorClassificationFactoryService : ILanguageServiceFactory
    {
        private readonly RoslynLSPClientServiceFactory roslynLSPClientServiceFactory;
        private readonly ClassificationTypeMap classificationTypeMap;

        [ImportingConstructor]
        public VBLspEditorClassificationFactoryService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, ClassificationTypeMap classificationTypeMap)
        {
            this.roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
            this.classificationTypeMap = classificationTypeMap ?? throw new ArgumentNullException(nameof(classificationTypeMap));
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new RoslynClassificationService(this.roslynLSPClientServiceFactory, languageServices.GetOriginalLanguageService<ISyntaxClassificationService>(), this.classificationTypeMap);
        }
    }
}
