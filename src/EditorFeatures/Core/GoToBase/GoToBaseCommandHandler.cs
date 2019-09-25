// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.GoToBase
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToBase)]
    internal class GoToBaseCommandHandler : AbstractGoToCommandHandler<IGoToBaseService, GoToBaseCommandArgs>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToBaseCommandHandler(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter) : base(threadingContext, streamingPresenter)
        {
        }

        public override string DisplayName => EditorFeaturesResources.Go_To_Base;

        protected override string _scopeDescription => EditorFeaturesResources.Locating_bases;

        protected override FunctionId _functionId => FunctionId.CommandHandler_GoToBase;

        protected override Task FindAction(IGoToBaseService service, Document document, int caretPosition, IFindUsagesContext context)
            => service.FindBasesAsync(document, caretPosition, context);
    }
}
