// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportLanguageServiceFactory(typeof(ITypeImportCompletionService), LanguageNames.CSharp), Shared]
    internal sealed class TypeImportCompletionServiceFactory : ILanguageServiceFactory
    {
        private readonly IAsynchronousOperationListenerProvider _provider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeImportCompletionServiceFactory(IAsynchronousOperationListenerProvider provider)
        {
            _provider = provider;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new CSharpTypeImportCompletionService(languageServices.WorkspaceServices.Workspace, _provider.GetListener(FeatureAttribute.CompletionSet));

        private class CSharpTypeImportCompletionService : AbstractTypeImportCompletionService
        {
            public CSharpTypeImportCompletionService(Workspace workspace, IAsynchronousOperationListener listener)
                : base(workspace, listener)
            {
            }

            protected override string GenericTypeSuffix
                => "<>";

            protected override bool IsCaseSensitive => true;

            protected override string Language => LanguageNames.CSharp;
        }
    }
}
