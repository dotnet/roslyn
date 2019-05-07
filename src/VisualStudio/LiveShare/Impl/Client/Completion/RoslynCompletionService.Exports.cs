using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
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
        private readonly VisualStudioWorkspace visualStudioWorkspace;

        [ImportingConstructor]
        public TypeScriptLspCompletionServiceFactory(VisualStudioWorkspace visualStudioWorkspace)
        {
            this.visualStudioWorkspace = visualStudioWorkspace ?? throw new ArgumentNullException(nameof(visualStudioWorkspace));
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            var originalService = this.visualStudioWorkspace.Services.GetLanguageServices(StringConstants.TypeScriptLanguageName).GetService<CompletionService>();
            return new RoslynCompletionService(languageServices.WorkspaceServices.Workspace,
                originalService, StringConstants.TypeScriptLanguageName);
        }
    }
}
