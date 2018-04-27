// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler
    {
        public VSCommanding.CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return GetCommandState(args.SubjectBuffer);
        }

        public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext context)
        {
            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return false;
            }

            RemoveSortUsings(args.SubjectBuffer.CurrentSnapshot, context);

            while (ApplyOneCodeFix(args.SubjectBuffer.CurrentSnapshot, context, out var hasMoreCodeFix) && hasMoreCodeFix) ;

            return FormatText(args.SubjectBuffer.CurrentSnapshot, args.TextView, context);
        }

        private bool RemoveSortUsings(ITextSnapshot currentSnapShot, CommandExecutionContext context)
        {
            var cancellationToken = context.WaitContext.UserCancellationToken;
            var document = currentSnapShot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            // remove and sort usings
            var removeUsingsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
            if (removeUsingsService == null)
            {
                return false;
            }

            using (context.WaitContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_document))
            {
                var newDoc = removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).Result;
                // sort usings
                newDoc = OrganizeImportsService.OrganizeImportsAsync(newDoc, cancellationToken).Result;
                var changes = newDoc.GetTextChangesAsync(document, cancellationToken).Result.ToList();

                if (changes.Count() > 0)
                {
                    ApplyChanges(document, changes, null, cancellationToken);
                }
            }
            return true;
        }

        private bool FormatText(ITextSnapshot currentSnapShot, ITextView textView, CommandExecutionContext context)
        {
            var document = currentSnapShot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var formattingService = document.GetLanguageService<IEditorFormattingService>();
            if (formattingService == null || !formattingService.SupportsFormatDocument)
            {
                return false;
            }

            using (context.WaitContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_document))
            {
                Format(textView, document, null, context.WaitContext.UserCancellationToken);
            }

            return true;
        }

        private bool ApplyOneCodeFix(ITextSnapshot currentSnapShot, CommandExecutionContext context, out bool hasMoreCodeFix)
        {
            hasMoreCodeFix = false;
            var cancellationToken = context.WaitContext.UserCancellationToken;

            // document needs to be retrieved again after each change, otherwise it does not work
            var document = currentSnapShot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var textSpan = new TextSpan(0, document.GetTextAsync().Result.Length);
            var fixCollectionArray = _codeFixService.GetFixesAsync(document, textSpan, true, cancellationToken).Result;
            if (fixCollectionArray != null)
            {
                foreach (var fixCollection in fixCollectionArray.Select(f => f.Fixes))
                {
                    foreach (var fix in fixCollection)
                    {
                        var codeAction = fix.Action;
                        // Hardcode to skip "Remove unused variable" code fix
                        if (codeAction.EquivalenceKey == "Remove unused variable") continue;

                        var ops = codeAction.GetOperationsAsync(cancellationToken).Result;
                        foreach (var op in ops)
                        {
                            // Apply one change at a time, setting hasMoreCodeFix to true as there are probably other code fixes
                            op.Apply(document.Project.Solution.Workspace, cancellationToken);
                            hasMoreCodeFix = true;
                            return true;
                        }
                    }
                }
            }

            return true;
        }
    }
}
