// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.LiveShare.WebEditors.ContainedLanguage;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Razor
{
    [Export(typeof(IContainedLanguageProvider))]
    internal class CSharpLspContainedLanguageProvider : IContainedLanguageProvider
    {
        private readonly IContentTypeRegistryService _contentTypeRegistry;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly CSharpLspRazorProjectFactory _razorProjectFactory;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspContainedLanguageProvider(IContentTypeRegistryService contentTypeRegistry,
            SVsServiceProvider serviceProvider,
            CSharpLspRazorProjectFactory razorProjectFactory)
        {
            _contentTypeRegistry = contentTypeRegistry ?? throw new ArgumentNullException(nameof(contentTypeRegistry));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _razorProjectFactory = razorProjectFactory ?? throw new ArgumentNullException(nameof(razorProjectFactory));
        }

        public IContentType GetContentType(string filePath)
            => _contentTypeRegistry.GetContentType(ContentTypeNames.CSharpContentType);

        public IVsContainedLanguage GetLanguage(string filePath, IVsTextBufferCoordinator bufferCoordinator)
        {
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
            var projectId = _razorProjectFactory.GetProject(filePath);

            return new ContainedLanguage(
                bufferCoordinator,
                componentModel,
                _razorProjectFactory.Workspace,
                projectId,
                project: null,
                Guids.CSharpLanguageServiceId);
        }
    }
}
