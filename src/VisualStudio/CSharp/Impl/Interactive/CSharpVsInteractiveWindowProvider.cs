// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.CSharp.Interactive;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using LanguageServiceGuids = Microsoft.VisualStudio.LanguageServices.Guids;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [Export(typeof(CSharpVsInteractiveWindowProvider))]
    internal sealed class CSharpVsInteractiveWindowProvider : VsInteractiveWindowProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _listener;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly EditorOptionsService _editorOptionsService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVsInteractiveWindowProvider(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IAsynchronousOperationListenerProvider listenerProvider,
            IVsInteractiveWindowFactory interactiveWindowFactory,
            IViewClassifierAggregatorService classifierAggregator,
            IContentTypeRegistryService contentTypeRegistry,
            IInteractiveWindowCommandsFactory commandsFactory,
            [ImportMany] IInteractiveWindowCommand[] commands,
            ITextDocumentFactoryService textDocumentFactoryService,
            EditorOptionsService editorOptionsService,
            VisualStudioWorkspace workspace)
            : base(serviceProvider, interactiveWindowFactory, classifierAggregator, contentTypeRegistry, commandsFactory, commands, workspace)
        {
            _threadingContext = threadingContext;
            _listener = listenerProvider.GetListener(FeatureAttribute.InteractiveEvaluator);
            _textDocumentFactoryService = textDocumentFactoryService;
            _editorOptionsService = editorOptionsService;
        }

        protected override Guid LanguageServiceGuid => LanguageServiceGuids.CSharpLanguageServiceId;

        protected override Guid Id => CSharpVsInteractiveWindowPackage.Id;

        protected override string Title => CSharpVSResources.CSharp_Interactive;

        protected override FunctionId InteractiveWindowFunctionId => FunctionId.CSharp_Interactive_Window;

        protected override CSharpInteractiveEvaluator CreateInteractiveEvaluator(
            SVsServiceProvider serviceProvider,
            IViewClassifierAggregatorService classifierAggregator,
            IContentTypeRegistryService contentTypeRegistry,
            VisualStudioWorkspace workspace)
        {
            return new CSharpInteractiveEvaluator(
                _threadingContext,
                _listener,
                contentTypeRegistry.GetContentType(ContentTypeNames.CSharpContentType),
                workspace.Services.HostServices,
                classifierAggregator,
                CommandsFactory,
                Commands,
                _textDocumentFactoryService,
                _editorOptionsService,
                CSharpInteractiveEvaluatorLanguageInfoProvider.Instance,
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }
    }
}
