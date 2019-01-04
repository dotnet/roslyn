// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup
{
    [ExportLanguageService(typeof(ICodeCleanupService), LanguageNames.CSharp), Shared]
    internal class CSharpCodeCleanupService : ICodeCleanupService
    {
        private readonly ICodeFixService _codeFixServiceOpt;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeCleanupService(
            // will remove the AllowDefault once CodeFixService is moved to Features
            // https://github.com/dotnet/roslyn/issues/27369
            [Import(AllowDefault = true)] ICodeFixService codeFixService)
        {
            _codeFixServiceOpt = codeFixService;
        }

        /// <summary>
        /// Maps format document code cleanup options to DiagnosticId[]
        /// </summary>
        private static ImmutableArray<(string description, PerLanguageOption<bool> option, ImmutableArray<string> diagnosticIds)> _optionDiagnosticsMappings =
            ImmutableArray.Create(
                (CSharpFeaturesResources.Apply_implicit_explicit_type_preferences,
                 CodeCleanupOptions.ApplyImplicitExplicitTypePreferences,
                 ImmutableArray.Create(IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                                       IDEDiagnosticIds.UseExplicitTypeDiagnosticId)),

                (CSharpFeaturesResources.Apply_this_qualification_preferences,
                 CodeCleanupOptions.ApplyThisQualificationPreferences,
                 ImmutableArray.Create(IDEDiagnosticIds.AddQualificationDiagnosticId,
                                       IDEDiagnosticIds.RemoveQualificationDiagnosticId)),

                (CSharpFeaturesResources.Apply_language_framework_type_preferences,
                 CodeCleanupOptions.ApplyLanguageFrameworkTypePreferences,
                 ImmutableArray.Create(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)),

                (CSharpFeaturesResources.Add_remove_braces_for_single_line_control_statements,
                 CodeCleanupOptions.AddRemoveBracesForSingleLineControlStatements,
                 ImmutableArray.Create(IDEDiagnosticIds.AddBracesDiagnosticId)),

                (CSharpFeaturesResources.Add_accessibility_modifiers,
                 CodeCleanupOptions.AddAccessibilityModifiers,
                 ImmutableArray.Create(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)),

                (CSharpFeaturesResources.Sort_accessibility_modifiers,
                 CodeCleanupOptions.SortAccessibilityModifiers,
                 ImmutableArray.Create(IDEDiagnosticIds.OrderModifiersDiagnosticId)),

                (CSharpFeaturesResources.Make_private_field_readonly_when_possible,
                 CodeCleanupOptions.MakePrivateFieldReadonlyWhenPossible,
                 ImmutableArray.Create(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)),

                (CSharpFeaturesResources.Remove_unnecessary_casts,
                 CodeCleanupOptions.RemoveUnnecessaryCasts,
                 ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)),

                (CSharpFeaturesResources.Apply_expression_block_body_preferences,
                 CodeCleanupOptions.ApplyExpressionBlockBodyPreferences,
                 ImmutableArray.Create(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                                       IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                                       IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                                       IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                                       IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                                       IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                                       IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                                       IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId)),

                (CSharpFeaturesResources.Apply_inline_out_variable_preferences,
                 CodeCleanupOptions.ApplyInlineOutVariablePreferences,
                 ImmutableArray.Create(IDEDiagnosticIds.InlineDeclarationDiagnosticId)),

                (CSharpFeaturesResources.Remove_unused_variables,
                 CodeCleanupOptions.RemoveUnusedVariables,
                 ImmutableArray.Create(CSharpRemoveUnusedVariableCodeFixProvider.CS0168,
                                       CSharpRemoveUnusedVariableCodeFixProvider.CS0219)),

                (CSharpFeaturesResources.Apply_object_collection_initialization_preferences,
                 CodeCleanupOptions.ApplyObjectCollectionInitializationPreferences,
                 ImmutableArray.Create(IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
                                       IDEDiagnosticIds.UseCollectionInitializerDiagnosticId))
            );

        public async Task<Document> CleanupAsync(
            Document document, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var docOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // add one item for the 'format' action we'll do last
            progressTracker.AddItems(1);

            // and one for 'remove/sort usings' if we're going to run that.
            var organizeUsings = docOptions.GetOption(CodeCleanupOptions.RemoveUnusedImports) ||
                                 docOptions.GetOption(CodeCleanupOptions.SortImports);

            if (organizeUsings)
            {
                progressTracker.AddItems(1);
            }

            if (_codeFixServiceOpt != null)
            {
                document = await ApplyCodeFixesAsync(
                    document, docOptions, progressTracker, cancellationToken).ConfigureAwait(false);
            }

            // do the remove usings after code fix, as code fix might remove some code which can results in unused usings.
            if (organizeUsings)
            {
                progressTracker.Description = CSharpFeaturesResources.Organize_Usings;
                document = await RemoveSortUsingsAsync(
                    document, docOptions, cancellationToken).ConfigureAwait(false);
                progressTracker.ItemCompleted();
            }

            progressTracker.Description = FeaturesResources.Formatting_document;
            using (Logger.LogBlock(FunctionId.CodeCleanup_Format, cancellationToken))
            {
                var result = await Formatter.FormatAsync(document).ConfigureAwait(false);
                progressTracker.ItemCompleted();
                return result;
            }
        }

        private async Task<Document> RemoveSortUsingsAsync(
            Document document, DocumentOptionSet docOptions, CancellationToken cancellationToken)
        {
            if (docOptions.GetOption(CodeCleanupOptions.RemoveUnusedImports))
            {
                var removeUsingsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                if (removeUsingsService != null)
                {
                    using (Logger.LogBlock(FunctionId.CodeCleanup_RemoveUnusedImports, cancellationToken))
                    {
                        document = await removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            if (docOptions.GetOption(CodeCleanupOptions.SortImports))
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_SortImports, cancellationToken))
                {
                    document = await OrganizeImportsService.OrganizeImportsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesAsync(
            Document document, DocumentOptionSet docOptions,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var enabledOptions = GetEnabledOptions(docOptions);

            // Add a progress item for each enabled option we're going to fixup.
            progressTracker.AddItems(enabledOptions.Length);

            foreach (var (description, diagnosticIds) in enabledOptions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progressTracker.Description = description;
                document = await ApplyCodeFixesForSpecificDiagnosticIds(
                    document, diagnosticIds, cancellationToken).ConfigureAwait(false);

                // Mark this option as being completed.
                progressTracker.ItemCompleted();
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesForSpecificDiagnosticIds(
            Document document, ImmutableArray<string> diagnosticIds, CancellationToken cancellationToken)
        {
            foreach (var diagnosticId in diagnosticIds)
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_ApplyCodeFixesAsync, diagnosticId, cancellationToken))
                {
                    document = await ApplyCodeFixesForSpecificDiagnosticId(
                        document, diagnosticId, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesForSpecificDiagnosticId(
            Document document, string diagnosticId, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = new TextSpan(0, tree.Length);

            var fixCollection = await _codeFixServiceOpt.GetDocumentFixAllForIdInSpan(
                document, textSpan, diagnosticId, cancellationToken).ConfigureAwait(false);
            if (fixCollection == null)
            {
                return document;
            }

            var fixAllService = document.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            var solution = await fixAllService.GetFixAllChangedSolutionAsync(
                fixCollection.FixAllState.CreateFixAllContext(new ProgressTracker(), cancellationToken)).ConfigureAwait(false);

            return solution.GetDocument(document.Id);
        }

        private ImmutableArray<(string description, ImmutableArray<string> diagnosticIds)> GetEnabledOptions(DocumentOptionSet docOptions)
        {
            var result = ArrayBuilder<(string description, ImmutableArray<string> diagnosticIds)>.GetInstance();

            foreach (var (description, option, diagnosticIds) in _optionDiagnosticsMappings)
            {
                if (docOptions.GetOption(option))
                {
                    result.AddRange((description, diagnosticIds));
                }
            }

            return result.ToImmutableAndFree();
        }
    }
}
