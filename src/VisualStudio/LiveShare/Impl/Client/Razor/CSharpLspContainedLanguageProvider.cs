// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.LiveShare.WebEditors.ContainedLanguage;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
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
        public CSharpLspContainedLanguageProvider(IContentTypeRegistryService contentTypeRegistry,
            SVsServiceProvider serviceProvider,
            CSharpLspRazorProjectFactory razorProjectFactory)
        {
            _contentTypeRegistry = contentTypeRegistry ?? throw new ArgumentNullException(nameof(contentTypeRegistry));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _razorProjectFactory = razorProjectFactory ?? throw new ArgumentNullException(nameof(razorProjectFactory));
        }

        public IContentType GetContentType(string filePath)
        {
            return GetContentType();
        }

        private IContentType GetContentType()
        {
            return _contentTypeRegistry.GetContentType(StringConstants.CSharpLspContentTypeName);
        }

        public IVsContainedLanguage GetLanguage(string filePath, IVsTextBufferCoordinator bufferCoordinator)
        {
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
            var projectId = _razorProjectFactory.GetProject(filePath);

            var containedLanguage = new ContainedLanguage(
                bufferCoordinator,
                componentModel,
                _razorProjectFactory.Workspace,
                projectId,
                project: null,
                filePath,
                Guids.CSharpLanguageServiceId);

            // Our buffer starts out as the regular CSharp content type. We want it to be C#_LSP. We can change it now, but
            // ASP.NET ultimately calls IVsContainedLanguage.GetLanguageServiceID to get our ID (which points to regular CSharp),
            // and switches the buffer back to that. By changing our content type now and then hooking up a buffer change watcher
            // we can change it back underneath ASP.NET.
            containedLanguage.SubjectBuffer.ChangeContentType(GetContentType(), editTag: nameof(CSharpLspContainedLanguageProvider) + "." + nameof(GetLanguage));
            containedLanguage.SubjectBuffer.ContentTypeChanged += SubjectBuffer_ContentTypeChanged;

            return containedLanguage;
        }

        private void SubjectBuffer_ContentTypeChanged(object sender, Text.ContentTypeChangedEventArgs e)
        {
            if (e.AfterContentType.TypeName == ContentTypeNames.CSharpContentType)
            {
                var buffer = (ITextBuffer)sender;
                buffer.ChangeContentType(GetContentType(), editTag: nameof(CSharpLspContainedLanguageProvider) + "." + nameof(SubjectBuffer_ContentTypeChanged));

                buffer.ContentTypeChanged -= SubjectBuffer_ContentTypeChanged;
            }
        }
    }
}
