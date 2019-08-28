// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Editor.CSharp.Interactive;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVsInteractiveWindowProvider(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IVsInteractiveWindowFactory interactiveWindowFactory,
            IViewClassifierAggregatorService classifierAggregator,
            IContentTypeRegistryService contentTypeRegistry,
            IInteractiveWindowCommandsFactory commandsFactory,
            [ImportMany]IInteractiveWindowCommand[] commands,
            VisualStudioWorkspace workspace)
            : base(serviceProvider, interactiveWindowFactory, classifierAggregator, contentTypeRegistry, commandsFactory, commands, workspace)
        {
            _threadingContext = threadingContext;
        }

        protected override Guid LanguageServiceGuid
        {
            get { return LanguageServiceGuids.CSharpLanguageServiceId; }
        }

        protected override Guid Id
        {
            get { return CSharpVsInteractiveWindowPackage.Id; }
        }

        protected override string Title
        {
            // Note: intentionally left unlocalized (we treat these words as if they were unregistered trademarks)
            get { return "C# Interactive"; }
        }

        protected override InteractiveEvaluator CreateInteractiveEvaluator(
            SVsServiceProvider serviceProvider,
            IViewClassifierAggregatorService classifierAggregator,
            IContentTypeRegistryService contentTypeRegistry,
            VisualStudioWorkspace workspace)
        {
            return new CSharpInteractiveEvaluator(
                _threadingContext,
                workspace.Services.HostServices,
                classifierAggregator,
                CommandsFactory,
                Commands,
                contentTypeRegistry,
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        protected override FunctionId InteractiveWindowFunctionId
        {
            get
            {
                return FunctionId.CSharp_Interactive_Window;
            }
        }
    }
}
