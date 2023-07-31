// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.GoToImplementation
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToImplementation)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class GoToImplementationCommandHandler(
        IThreadingContext threadingContext,
        IStreamingFindUsagesPresenter streamingPresenter,
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        IAsynchronousOperationListenerProvider listenerProvider,
        IGlobalOptionService globalOptions) : AbstractGoToCommandHandler<IFindUsagesService, GoToImplementationCommandArgs>(threadingContext,
               streamingPresenter,
               uiThreadOperationExecutor,
               listenerProvider.GetListener(FeatureAttribute.GoToImplementation),
               globalOptions)
    {
        public override string DisplayName => EditorFeaturesResources.Go_To_Implementation;

        protected override string ScopeDescription => EditorFeaturesResources.Locating_implementations;
        protected override FunctionId FunctionId => FunctionId.CommandHandler_GoToImplementation;

        protected override Task FindActionAsync(IFindUsagesContext context, Document document, int caretPosition, CancellationToken cancellationToken)
            => document.GetRequiredLanguageService<IFindUsagesService>()
                       .FindImplementationsAsync(context, document, caretPosition, cancellationToken);
    }
}
