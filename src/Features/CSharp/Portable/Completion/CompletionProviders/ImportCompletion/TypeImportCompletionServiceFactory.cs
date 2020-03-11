// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportLanguageServiceFactory(typeof(ITypeImportCompletionService), LanguageNames.CSharp), Shared]
    internal sealed class TypeImportCompletionServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public TypeImportCompletionServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpTypeImportCompletionService(languageServices.WorkspaceServices.Workspace);
        }

        private class CSharpTypeImportCompletionService : AbstractTypeImportCompletionService
        {
            public CSharpTypeImportCompletionService(Workspace workspace)
                : base(workspace)
            {
            }

            protected override string GenericTypeSuffix
                => "<>";

            protected override bool IsCaseSensitive => true;
        }
    }
}
