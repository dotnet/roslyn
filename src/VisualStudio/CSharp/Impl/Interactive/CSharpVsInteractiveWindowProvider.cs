﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.CSharp.Interactive;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using LanguageServiceGuids = Microsoft.VisualStudio.LanguageServices.Guids;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [Export(typeof(CSharpVsInteractiveWindowProvider))]
    internal sealed class CSharpVsInteractiveWindowProvider : VsInteractiveWindowProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _listener;

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
            VisualStudioWorkspace workspace)
            : base(serviceProvider, interactiveWindowFactory, classifierAggregator, contentTypeRegistry, commandsFactory, commands, workspace)
        {
            _threadingContext = threadingContext;
            _listener = listenerProvider.GetListener(FeatureAttribute.InteractiveEvaluator);
        }

        protected override Guid LanguageServiceGuid => LanguageServiceGuids.CSharpLanguageServiceId;

        protected override Guid Id => CSharpVsInteractiveWindowPackage.Id;

        // Note: intentionally left unlocalized (we treat these words as if they were unregistered trademarks)
        protected override string Title => "C# Interactive";

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
                CSharpInteractiveEvaluatorLanguageInfoProvider.Instance,
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }
    }
}
