using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text;
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
        private IBraceMatchingService _braceMatchingService;

        [ImportingConstructor]
        internal GoToMatchingBraceCommandHandler(IBraceMatchingService braceMatchingService)
        {
            _braceMatchingService = braceMatchingService ??
                throw new ArgumentNullException(nameof(braceMatchingService));
        }

        public string DisplayName => nameof(GoToMatchingBraceCommandHandler);

        public bool ExecuteCommand(GotoBraceCommandArgs args, VSCommanding.CommandExecutionContext executionContext)
        {
            ITextSnapshot snapshot = args.SubjectBuffer.CurrentSnapshot;
            Document document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            var caretPosition = args.TextView.Caret.Position.BufferPosition.Position;

            var task = _braceMatchingService.FindMatchingSpanAsync(document, caretPosition, executionContext.OperationContext.UserCancellationToken);
            var span = task.WaitAndGetResult(executionContext.OperationContext.UserCancellationToken);

            if (!span.HasValue) return false;

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
