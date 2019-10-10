// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Debugging;
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
        private readonly RemoteLanguageServiceWorkspace _remoteLanguageServiceWorkspace;
        private readonly CSharpLspRazorProject _razorProject;

        [ImportingConstructor]
        public CSharpLspContainedLanguageProvider(IContentTypeRegistryService contentTypeRegistry,
            SVsServiceProvider serviceProvider,
            CSharpLspRazorProject razorProject,
            RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace)
        {
            _contentTypeRegistry = contentTypeRegistry ?? throw new ArgumentNullException(nameof(contentTypeRegistry));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _razorProject = razorProject ?? throw new ArgumentNullException(nameof(razorProject));
            _remoteLanguageServiceWorkspace = remoteLanguageServiceWorkspace ?? throw new ArgumentNullException(nameof(remoteLanguageServiceWorkspace));
        }

        public IContentType GetContentType(string filePath)
        {
            return _contentTypeRegistry.GetContentType(StringConstants.CSharpLspContentTypeName);
        }

        public IVsContainedLanguage GetLanguage(string filePath, IVsTextBufferCoordinator bufferCoordinator)
        {
            var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var project = _razorProject.GetProject(filePath);

            var languageService = CSharpLspLanguageService.FromServiceProvider(_serviceProvider);
#pragma warning disable CS0618 // Type or member is obsolete - this is liveshare.
            return new ContainedLanguage<CSharpLspPackage, CSharpLspLanguageService>(bufferCoordinator,
                componentModel,
                project,
                new RazorProjectHierarchy(filePath),
                (uint)VSConstants.VSITEMID.Nil,
                languageService,
                CodeAnalysis.SourceCodeKind.Regular,
                vbHelperFormattingRule: null,
                workspace: _remoteLanguageServiceWorkspace);
#pragma warning restore CS0618 // Type or member is obsolete - this is liveshare.
        }
    }
}
