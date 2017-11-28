// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.GoToDisassembly
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.GoToDisassembly,
        ContentTypeNames.RoslynContentType)]
    internal partial class GoToDisassemblyCommandHandler : ICommandHandler<GoToDisassemblyCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _streamingPresenters;

        [ImportingConstructor]
        public GoToDisassemblyCommandHandler(
            IWaitIndicator waitIndicator,
            [ImportMany] IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters)
        {
            _waitIndicator = waitIndicator;
            _streamingPresenters = streamingPresenters;
        }

        public CommandState GetCommandState(GoToDisassemblyCommandArgs args, Func<CommandState> nextHandler)
        {
            // Because this is expensive to compute, we just always say yes as long as the language allows it.
            return CommandState.Available;
        }

        public void ExecuteCommand(GoToDisassemblyCommandArgs args, Action nextHandler)
        {
            var document = args.TextView.TextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();


            nextHandler();
        }

        private void Do()
        {
            
        }
    }
}
