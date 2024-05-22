// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Formatting;

internal partial class FormatCommandHandler
{
    public CommandState GetCommandState(FormatDocumentCommandArgs args)
        => GetCommandState(args.SubjectBuffer);

    public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext context)
    {
        if (!CanExecuteCommand(args.SubjectBuffer))
        {
            return false;
        }

        var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
        {
            return false;
        }

        var formattingService = document.GetLanguageService<IFormattingInteractionService>();
        if (formattingService == null || !formattingService.SupportsFormatDocument)
        {
            return false;
        }

        using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_document))
        {
            Format(args.TextView, args.SubjectBuffer, document, selectionOpt: null, context.OperationContext.UserCancellationToken);
        }

        return true;
    }
}
