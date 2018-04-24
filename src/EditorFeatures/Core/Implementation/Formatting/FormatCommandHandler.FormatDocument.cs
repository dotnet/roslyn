// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
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
            return TryExecuteCommand(args, context);
        }

        private bool TryExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext context)
        {
            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return false;
            }

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var cancellationToken = context.WaitContext.UserCancellationToken;

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

            // remove unused variables
            var fixCollectionArray = _codeFixService.GetFixesAsync(document, new TextSpan(0, document.GetTextAsync().Result.Length), true, cancellationToken).Result;
            if (fixCollectionArray != null)
            {
                foreach (var fixCollection in fixCollectionArray.Select(f => f.Fixes))
                {
                    foreach (var fix in fixCollection)
                    {
                        var ops = fix.Action.GetPreviewOperationsAsync(cancellationToken).Result;
                        foreach (var op in ops)
                        {
                            op.Apply(document.Project.Solution.Workspace, cancellationToken);
                        }
                    }
                }
            }

            // formatting
            var formattingService = document.GetLanguageService<IEditorFormattingService>();
            if (formattingService == null || !formattingService.SupportsFormatDocument)
            {
                return false;
            }

            using (context.WaitContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_document))
            {
                Format(args.TextView, document, null, context.WaitContext.UserCancellationToken);
            }


            return true;
        }
    }
}
