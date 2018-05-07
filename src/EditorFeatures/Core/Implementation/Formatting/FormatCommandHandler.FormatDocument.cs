// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;
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
            try
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

                using (context.WaitContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_document))
                {
                    var cancellationToken = context.WaitContext.UserCancellationToken;

                    var oldDocument = document;
                    using (var transaction = new CaretPreservingEditTransaction(
                        EditorFeaturesResources.Formatting, args.TextView, _undoHistoryRegistry, _editorOperationsFactoryService))
                    {
                        document = RemoveSortUsings(document, cancellationToken);
                        document = ApplyCodeFixes(document, cancellationToken);

                        var codeFixChanges = document.GetTextChangesAsync(oldDocument, cancellationToken).WaitAndGetResult(cancellationToken).ToList();

                        // we should do apply changes only once. but for now, we just do it twice, for all others and formatting
                        if (codeFixChanges.Count > 0)
                        {
                            ApplyChanges(oldDocument, codeFixChanges, selectionOpt: null, cancellationToken);
                            transaction.Complete();
                        }
                    }

                    // this call into existing one
                    FormatText(args.SubjectBuffer.CurrentSnapshot, args.TextView, cancellationToken);
                    return true;
                }
            }
            catch
            {
                // this is just for demo, we just making not crashing.
                return false;
            }
        }

        private Document RemoveSortUsings(Document document, CancellationToken cancellationToken)
        {
            // remove and sort usings
            var removeUsingsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
            if (removeUsingsService == null)
            {
                return document;
            }

            var newDocument = removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);

            // sort usings
            return OrganizeImportsService.OrganizeImportsAsync(newDocument, cancellationToken).WaitAndGetResult(cancellationToken);
        }

        private bool FormatText(ITextSnapshot currentSnapShot, ITextView textView, CancellationToken cancellationToken)
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

            Format(textView, document, null, cancellationToken);
            return true;
        }

        private Document ApplyCodeFixes(Document document, CancellationToken cancellationToken)
        {
            var fixAllService = document.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            var dummy = new ProgressTracker();
            foreach (var diagnosticId in new string[] {
                IDEDiagnosticIds.AddBracesDiagnosticId,
                IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId,
                IDEDiagnosticIds.OrderModifiersDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                IDEDiagnosticIds.AddQualificationDiagnosticId,
                IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId,
                IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId,
                IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId,
                IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId,
                IDEDiagnosticIds.InlineDeclarationDiagnosticId,
                IDEDiagnosticIds.InlineAsTypeCheckId,
                IDEDiagnosticIds.InlineIsTypeCheckId,
                IDEDiagnosticIds.InlineIsTypeWithoutNameCheckDiagnosticsId
            })
            {
                var length = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken).Length;
                var textSpan = new TextSpan(0, length);

                var fixCollectionArray = _codeFixService.GetFixesAsync(document, diagnosticId, textSpan, cancellationToken).WaitAndGetResult(cancellationToken);
                if (fixCollectionArray == null || fixCollectionArray.Length == 0)
                {
                    continue;
                }

                var fixAll = fixCollectionArray.First().FixAllState;
                var solution = fixAllService.GetFixAllChangedSolutionAsync(fixAll.CreateFixAllContext(dummy, cancellationToken)).WaitAndGetResult(cancellationToken);

                document = solution.GetDocument(document.Id);
            }

            return document;
        }
    }
}
