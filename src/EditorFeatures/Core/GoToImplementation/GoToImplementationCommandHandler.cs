﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToImplementation
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToImplementation)]
    internal class GoToImplementationCommandHandler : AbstractGoToCommandHandler<IFindUsagesService, GoToImplementationCommandArgs>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToImplementationCommandHandler(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter) : base(threadingContext, streamingPresenter)
        {
        }

        public override string DisplayName => EditorFeaturesResources.Go_To_Implementation;

        protected override string ScopeDescription => EditorFeaturesResources.Locating_implementations;

        protected override FunctionId FunctionId => FunctionId.CommandHandler_GoToImplementation;

        protected override Task FindAction(IFindUsagesService service, Document document, int caretPosition, IFindUsagesContext context)
            => service.FindImplementationsAsync(document, caretPosition, context);
    }
}
