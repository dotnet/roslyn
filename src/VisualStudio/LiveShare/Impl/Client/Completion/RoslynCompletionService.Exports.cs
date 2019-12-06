// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Completion
{
    [ExportLanguageServiceFactory(typeof(CompletionService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspCompletionServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            var originalService = languageServices.GetOriginalLanguageService<CompletionService>();
            return new RoslynCompletionService(languageServices.WorkspaceServices.Workspace,
                originalService, StringConstants.CSharpLspLanguageName);
        }
    }

    [ExportLanguageServiceFactory(typeof(CompletionService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspCompletionServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            var originalService = languageServices.GetOriginalLanguageService<CompletionService>();
            return new RoslynCompletionService(languageServices.WorkspaceServices.Workspace,
                originalService, StringConstants.VBLspLanguageName);
        }
    }

    // In d16, this export is to disable local TypeScript completion within a Live Share session; it is not used
    [ExportLanguageServiceFactory(typeof(CompletionService), StringConstants.TypeScriptLanguageName, WorkspaceKind.AnyCodeRoslynWorkspace), Shared]
    internal class TypeScriptLspCompletionServiceFactory : ILanguageServiceFactory
    {
        private readonly VisualStudioWorkspace _visualStudioWorkspace;

        [ImportingConstructor]
        public TypeScriptLspCompletionServiceFactory(VisualStudioWorkspace visualStudioWorkspace)
        {
            _visualStudioWorkspace = visualStudioWorkspace ?? throw new ArgumentNullException(nameof(visualStudioWorkspace));
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            var originalService = _visualStudioWorkspace.Services.GetLanguageServices(StringConstants.TypeScriptLanguageName).GetService<CompletionService>();
            return new RoslynCompletionService(languageServices.WorkspaceServices.Workspace,
                originalService, StringConstants.TypeScriptLanguageName);
        }
    }
}
