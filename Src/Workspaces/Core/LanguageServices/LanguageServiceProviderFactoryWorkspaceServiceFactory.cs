// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
#if MEF
using System.ComponentModel.Composition;
#endif
using System.Linq;
#if !MEF
using Microsoft.CodeAnalysis.Composition;
#endif
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
#if MEF
    [ExportWorkspaceServiceFactory(typeof(ILanguageServiceProviderFactory), WorkspaceKind.Any)]
    internal class LanguageServiceProviderFactoryWorkspaceServiceFactory : IWorkspaceServiceFactory, IPartImportsSatisfiedNotification
#else
    internal class LanguageServiceProviderFactoryWorkspaceServiceFactory : IWorkspaceServiceFactory
#endif
    {
#if MEF
        [ImportMany]
#endif
        private IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> languageServices = null;

#if MEF
        [ImportMany]
#endif
        private IEnumerable<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>> languageServiceFactories = null;

        private Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>> unboundServices;

#if MEF
        public void OnImportsSatisfied()
        {
            this.unboundServices = new Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>>(
                () => languageServices.Select(ls => new KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>(ls.Metadata, (lsp) => ls.Value))
                        .Concat(languageServiceFactories.Select(lsf => new KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>(lsf.Metadata, (lsp) => lsf.Value.CreateLanguageService(lsp)))).ToImmutableList());
        }

        public LanguageServiceProviderFactoryWorkspaceServiceFactory()
        {
        }
#else

        public LanguageServiceProviderFactoryWorkspaceServiceFactory(ExportSource exports)
        {
            this.languageServices = exports.GetExports<ILanguageService, LanguageServiceMetadata>();
            this.languageServiceFactories = exports.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>();

            this.unboundServices = new Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>>(
                () => languageServices.Select(ls => new KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>(ls.Metadata, (lsp) => ls.Value))
                        .Concat(languageServiceFactories.Select(lsf => new KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>(lsf.Metadata, (lsp) => lsf.Value.CreateLanguageService(lsp)))).ToImmutableList());
        }
#endif

        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new LanguageServiceProviderFactory(workspaceServices, this.unboundServices.Value);
        }
    }
}