// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(GoToMatchingBraceCommandHandler))]
    internal class GoToMatchingBraceCommandHandler : VSCommanding.ICommandHandler<GotoBraceCommandArgs>
    {
        private readonly IBraceMatchingService _braceMatchingService;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToMatchingBraceCommandHandler(IBraceMatchingService braceMatchingService, IGlobalOptionService globalOptions)
        {
            _braceMatchingService = braceMatchingService;
            _globalOptions = globalOptions;
        }

        public string DisplayName => nameof(GoToMatchingBraceCommandHandler);

        public bool ExecuteCommand(GotoBraceCommandArgs args, VSCommanding.CommandExecutionContext executionContext)
        {
            var snapshot = args.SubjectBuffer.CurrentSnapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            var options = _globalOptions.GetBraceMatchingOptions(document.Project.Language);

            var caretPosition = args.TextView.Caret.Position.BufferPosition.Position;
            var task = _braceMatchingService.FindMatchingSpanAsync(document, caretPosition, options, executionContext.OperationContext.UserCancellationToken);
            var span = task.WaitAndGetResult(executionContext.OperationContext.UserCancellationToken);

            if (!span.HasValue)
                return false;

            if (span.Value.Start < caretPosition)
                args.TextView.TryMoveCaretToAndEnsureVisible(args.SubjectBuffer.CurrentSnapshot.GetPoint(span.Value.Start));
            else if (span.Value.End > caretPosition)
                args.TextView.TryMoveCaretToAndEnsureVisible(args.SubjectBuffer.CurrentSnapshot.GetPoint(span.Value.End));

            return true;
        }

        public VSCommanding.CommandState GetCommandState(GotoBraceCommandArgs args)
        {
            return VSCommanding.CommandState.Available;
        }
    }
}
