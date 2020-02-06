﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification
{
    [ExportLanguageServiceFactory(typeof(IClassificationService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspClassificationServiceFactory : RoslynClassificationServiceFactory
    {
        protected override string LiveShareContentType => StringConstants.CSharpLspLanguageName;
    }

    [ExportLanguageServiceFactory(typeof(IClassificationService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspClassificationServiceFactory : RoslynClassificationServiceFactory
    {
        protected override string LiveShareContentType => StringConstants.VBLspLanguageName;
    }

    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspEditorClassificationFactoryService : RoslynSyntaxClassificationServiceFactory
    {
        [ImportingConstructor]
        public CSharpLspEditorClassificationFactoryService(CSharpLspClientServiceFactory csharpLspClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
            : base(csharpLspClientServiceFactory, remoteLanguageServiceWorkspace, classificationTypeMap, threadingContext)
        {
        }
    }

    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspEditorClassificationFactoryService : RoslynSyntaxClassificationServiceFactory
    {
        [ImportingConstructor]
        public VBLspEditorClassificationFactoryService(VisualBasicLspClientServiceFactory vbLspClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
            : base(vbLspClientServiceFactory, remoteLanguageServiceWorkspace, classificationTypeMap, threadingContext)
        {
        }
    }
}
