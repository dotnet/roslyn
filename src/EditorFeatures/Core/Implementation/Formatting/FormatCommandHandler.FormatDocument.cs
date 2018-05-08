// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
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
        private IDictionary<PerLanguageOption<bool>, string[]> _formatDocumentOptionMapping = GetFormatDocumentOptionMapping();

        private static IDictionary<PerLanguageOption<bool>, string[]> GetFormatDocumentOptionMapping()
        {
            var formatDocumentOptionMapping = new Dictionary<PerLanguageOption<bool>, string[]>();
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixImplicitExplicitType,
                new[] { IDEDiagnosticIds.UseImplicitTypeDiagnosticId, IDEDiagnosticIds.UseExplicitTypeDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixThisQualification,
                new[] { IDEDiagnosticIds.AddQualificationDiagnosticId, IDEDiagnosticIds.RemoveQualificationDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixFrameworkTypes,
                new[] { IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId, IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixAddRemoveBraces,
                new[] { IDEDiagnosticIds.AddBracesDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixAccessibilityModifiers,
                new[] { IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.SortAccessibilityModifiers,
                new[] { IDEDiagnosticIds.OrderModifiersDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.MakeReadonly,
                new[] { IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.RemoveUnnecessaryCasts,
                new[] { IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixExpressionBodiedMembers,
                new[] { IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixInlineVariableDeclarations,
                new[] { IDEDiagnosticIds.InlineDeclarationDiagnosticId });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.RemoveUnusedVariables,
                new[] { "CS0168", "CS0219" });
            formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixObjectCollectionInitialization,
                new[] { IDEDiagnosticIds.UseObjectInitializerDiagnosticId, IDEDiagnosticIds.UseCollectionInitializerDiagnosticId });
            //formatDocumentOptionMapping.Add(FeatureOnOffOptions.FixLanguageFeatures,
            //    new[] { IDEDiagnosticIds. });
            //IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId,
            //    IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId,
            //    IDEDiagnosticIds.InlineAsTypeCheckId,
            //    IDEDiagnosticIds.InlineIsTypeCheckId,
            //    IDEDiagnosticIds.InlineIsTypeWithoutNameCheckDiagnosticsId,

            return formatDocumentOptionMapping;
        }

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
                    var optionService = document.Project.Solution.Workspace.Services.GetService<IOptionService>();

                    document = RemoveSortUsings(document, optionService, cancellationToken);
                    document = ApplyCodeFixes(document, optionService, cancellationToken);

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

        private Document RemoveSortUsings(Document document, IOptionService optionService, CancellationToken cancellationToken)
        {
            // remove and sort usings
            if (optionService.GetOption(FeatureOnOffOptions.RemoveUnusedUsings, LanguageNames.CSharp))
            {
                var removeUsingsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                if (removeUsingsService != null)
                {
                    document = removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                }
            }

            // sort usings
            if (optionService.GetOption(FeatureOnOffOptions.SortUsings, LanguageNames.CSharp))
            {
                document = OrganizeImportsService.OrganizeImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
            }

            return document;
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

        private List<string> GetEnabledDiagnosticIds(IOptionService optionService)
        {
            var diagnosticIds = new List<string>();

            foreach (var featureOption in _formatDocumentOptionMapping.Keys)
            {
                if (optionService.GetOption(featureOption, LanguageNames.CSharp))
                {
                    diagnosticIds.AddRange(_formatDocumentOptionMapping[featureOption]);
                }
            }

            return diagnosticIds;
        }

        private Document ApplyCodeFixes(Document document, IOptionService optionService, CancellationToken cancellationToken)
        {
            var fixAllService = document.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            var dummy = new ProgressTracker();
            foreach (var diagnosticId in GetEnabledDiagnosticIds(optionService))
            {
                var length = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken).Length;
                var textSpan = new TextSpan(0, length);

                var fixCollectionArray = _codeFixService.GetFixesAsync(document, diagnosticId, textSpan, cancellationToken).WaitAndGetResult(cancellationToken);
                if (fixCollectionArray == null || fixCollectionArray.Length == 0)
                {
                    continue;
                }

                // TODO: Just apply the first fix for now until we have a way to config user's preferred fix
                var fixAll = fixCollectionArray.First().FixAllState;

                var solution = fixAllService.GetFixAllChangedSolutionAsync(fixAll.CreateFixAllContext(dummy, cancellationToken)).WaitAndGetResult(cancellationToken);

                document = solution.GetDocument(document.Id);
            }

            return document;
        }
    }
}
