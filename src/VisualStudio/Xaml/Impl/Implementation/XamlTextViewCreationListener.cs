// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed partial class XamlTextViewCreationListener : IWpfTextViewCreationListener
    {
        // Temporary UIConext GUID owned by the XAML language service until we can get a KnownUIContext
        private static readonly Guid s_serverUIContextGuid = new Guid("39F55746-6E65-4FCF-BEC5-EC0B466EAC0F");

        private readonly IServiceProvider _serviceProvider;
        private readonly XamlProjectService _projectService;
        private readonly UIContext _serverUIContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlTextViewCreationListener(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            XamlProjectService projectService)
        {
            _serviceProvider = serviceProvider;
            _projectService = projectService;
            _serverUIContext = UIContext.FromUIContextGuid(s_serverUIContextGuid);
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (_serverUIContext.IsActive)
            {
                return;
            }

            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            var filePath = textView.GetFilePath();

            _projectService.TrackOpenDocument(filePath);

            var target = new XamlOleCommandTarget(textView, (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel)));
            target.AttachToVsTextView();
        }
    }
}
