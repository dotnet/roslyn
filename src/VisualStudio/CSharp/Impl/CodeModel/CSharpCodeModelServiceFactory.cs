﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeModelServiceFactory(
            IEditorOptionsFactoryService editorOptionsFactoryService,
            [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            IThreadingContext threadingContext)
        {
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _refactorNotifyServices = refactorNotifyServices;
            _threadingContext = threadingContext;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
            => new CSharpCodeModelService(provider, _editorOptionsFactoryService, _refactorNotifyServices, _threadingContext);
    }
}
