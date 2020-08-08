// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification
{
    [ExportLanguageServiceFactory(typeof(IClassificationService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspClassificationServiceFactory : RoslynClassificationServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspClassificationServiceFactory()
        {
        }

        protected override string LiveShareContentType => StringConstants.CSharpLspLanguageName;
    }

    [ExportLanguageServiceFactory(typeof(IClassificationService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspClassificationServiceFactory : RoslynClassificationServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VBLspClassificationServiceFactory()
        {
        }

        protected override string LiveShareContentType => StringConstants.VBLspLanguageName;
    }

    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspEditorClassificationFactoryService : RoslynSyntaxClassificationServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspEditorClassificationFactoryService(CSharpLspClientServiceFactory csharpLspClientServiceFactory,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
            : base(csharpLspClientServiceFactory, classificationTypeMap, threadingContext)
        {
        }
    }

    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspEditorClassificationFactoryService : RoslynSyntaxClassificationServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VBLspEditorClassificationFactoryService(VisualBasicLspClientServiceFactory vbLspClientServiceFactory,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
            : base(vbLspClientServiceFactory, classificationTypeMap, threadingContext)
        {
        }
    }
}
