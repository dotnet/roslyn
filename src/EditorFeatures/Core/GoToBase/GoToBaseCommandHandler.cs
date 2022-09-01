// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.GoToBase
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToBase)]
    internal sealed class GoToBaseCommandHandler : AbstractGoToCommandHandler<IGoToBaseService, GoToBaseCommandArgs>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToBaseCommandHandler(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter,
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            IAsynchronousOperationListenerProvider listenerProvider,
            IGlobalOptionService globalOptions)
            : base(threadingContext,
                   streamingPresenter,
                   uiThreadOperationExecutor,
                   listenerProvider.GetListener(FeatureAttribute.GoToBase),
                   globalOptions)
        {
        }

        public override string DisplayName => EditorFeaturesResources.Go_To_Base;

        protected override string ScopeDescription => EditorFeaturesResources.Locating_bases;
        protected override FunctionId FunctionId => FunctionId.CommandHandler_GoToBase;

        protected override Task FindActionAsync(IFindUsagesContext context, Document document, int caretPosition, CancellationToken cancellationToken)
            => document.GetRequiredLanguageService<IGoToBaseService>()
                       .FindBasesAsync(context, document, caretPosition, cancellationToken);
    }
}
