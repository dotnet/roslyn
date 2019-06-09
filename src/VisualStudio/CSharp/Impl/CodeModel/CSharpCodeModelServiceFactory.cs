// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    [ExportLanguageServiceFactory(typeof(ICodeModelService), LanguageNames.CSharp), Shared]
    internal partial class CSharpCodeModelServiceFactory : ILanguageServiceFactory
    {
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        [ImportingConstructor]
        public CSharpCodeModelServiceFactory(
            IEditorOptionsFactoryService editorOptionsFactoryService,
            [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices)
        {
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _refactorNotifyServices = refactorNotifyServices;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpCodeModelService(provider, _editorOptionsFactoryService, _refactorNotifyServices);
        }
    }
}
