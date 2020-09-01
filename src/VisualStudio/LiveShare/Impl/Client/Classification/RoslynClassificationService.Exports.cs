// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.VisualBasic.Classification;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification
{
    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), LanguageNames.CSharp, WorkspaceKind.CloudEnvironmentClientWorkspace), Shared]
    internal class CSharpLspEditorClassificationFactory : RoslynSyntaxClassificationServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspEditorClassificationFactory(CSharpLspClientServiceFactory csharpLspClientServiceFactory,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
            : base(csharpLspClientServiceFactory, classificationTypeMap, threadingContext)
        {
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        protected override ISyntaxClassificationService GetOriginalSyntaxClassificationService(HostLanguageServices languageServices)
            => new CSharpSyntaxClassificationService(languageServices);
    }

    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), LanguageNames.VisualBasic, WorkspaceKind.CloudEnvironmentClientWorkspace), Shared]
    internal class VBLspEditorClassificationServiceFactory : RoslynSyntaxClassificationServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VBLspEditorClassificationServiceFactory(VisualBasicLspClientServiceFactory vbLspClientServiceFactory,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
            : base(vbLspClientServiceFactory, classificationTypeMap, threadingContext)
        {
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        protected override ISyntaxClassificationService GetOriginalSyntaxClassificationService(HostLanguageServices languageServices)
            => new VisualBasicSyntaxClassificationService(languageServices);
    }
}
