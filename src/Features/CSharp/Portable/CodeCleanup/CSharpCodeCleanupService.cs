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
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

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
        private static readonly ImmutableArray<DiagnosticSet> s_diagnosticSets =
            ImmutableArray.Create(
                new DiagnosticSet(CSharpFeaturesResources.Apply_implicit_explicit_type_preferences,
                    new[] { IDEDiagnosticIds.UseImplicitTypeDiagnosticId, IDEDiagnosticIds.UseExplicitTypeDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Apply_this_qualification_preferences,
                    new[] { IDEDiagnosticIds.AddQualificationDiagnosticId, IDEDiagnosticIds.RemoveQualificationDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Apply_language_framework_type_preferences,
                    new[] { IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Add_remove_braces_for_single_line_control_statements,
                    new[] { IDEDiagnosticIds.AddBracesDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Add_accessibility_modifiers,
                    new[] { IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Sort_accessibility_modifiers,
                    new[] { IDEDiagnosticIds.OrderModifiersDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Make_private_field_readonly_when_possible,
                    new[] { IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Remove_unnecessary_casts,
                    new[] { IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Apply_expression_block_body_preferences,
                    new[] {IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                            IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                            IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                            IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                            IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                            IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                            IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                            IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId}),

                new DiagnosticSet(CSharpFeaturesResources.Apply_inline_out_variable_preferences,
                    new[] { IDEDiagnosticIds.InlineDeclarationDiagnosticId }),

                new DiagnosticSet(CSharpFeaturesResources.Remove_unused_variables,
                    new[] { CSharpRemoveUnusedVariableCodeFixProvider.CS0168, CSharpRemoveUnusedVariableCodeFixProvider.CS0219 }),

                new DiagnosticSet(CSharpFeaturesResources.Apply_object_collection_initialization_preferences,
                    new[] { IDEDiagnosticIds.UseObjectInitializerDiagnosticId, IDEDiagnosticIds.UseCollectionInitializerDiagnosticId })
            );

        public async Task<Document> CleanupAsync(
            Document document,
            EnabledDiagnosticOptions enabledDiagnostics,
            IProgressTracker progressTracker,
            CancellationToken cancellationToken)
        {
            // add one item for the 'format' action we'll do last
            progressTracker.AddItems(1);

            // and one for 'remove/sort usings' if we're going to run that.
            var organizeUsings = enabledDiagnostics.OrganizeUsings.IsRemoveUnusedImportEnabled ||
                enabledDiagnostics.OrganizeUsings.IsSortImportsEnabled;
            if (organizeUsings)
            {
                progressTracker.AddItems(1);
            }

            if (_codeFixServiceOpt != null)
            {
                document = await ApplyCodeFixesAsync(
                    document, enabledDiagnostics.Diagnostics, progressTracker, cancellationToken).ConfigureAwait(false);
            }

            // do the remove usings after code fix, as code fix might remove some code which can results in unused usings.
            if (organizeUsings)
            {
                progressTracker.Description = CSharpFeaturesResources.Organize_Usings;
                document = await RemoveSortUsingsAsync(
                    document, enabledDiagnostics.OrganizeUsings, cancellationToken).ConfigureAwait(false);
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
            Document document, OrganizeUsingsSet organizeUsingsSet, CancellationToken cancellationToken)
        {
            if (organizeUsingsSet.IsRemoveUnusedImportEnabled)
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

            if (organizeUsingsSet.IsSortImportsEnabled)
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_SortImports, cancellationToken))
                {
                    document = await OrganizeImportsService.OrganizeImportsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesAsync(
            Document document, ImmutableArray<DiagnosticSet> enabledDiagnosticSets,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            // Add a progress item for each enabled option we're going to fixup.
            progressTracker.AddItems(enabledDiagnosticSets.Length);

            foreach (var diagnosticSet in enabledDiagnosticSets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progressTracker.Description = diagnosticSet.Description;
                document = await ApplyCodeFixesForSpecificDiagnosticIdsAsync(
                    document, diagnosticSet.DiagnosticIds, progressTracker, cancellationToken).ConfigureAwait(false);

                // Mark this option as being completed.
                progressTracker.ItemCompleted();
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesForSpecificDiagnosticIdsAsync(
            Document document, ImmutableArray<string> diagnosticIds, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            foreach (var diagnosticId in diagnosticIds)
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_ApplyCodeFixesAsync, diagnosticId, cancellationToken))
                {
                    document = await _codeFixServiceOpt.ApplyCodeFixesForSpecificDiagnosticIdAsync(
                        document, diagnosticId, progressTracker, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }

        public EnabledDiagnosticOptions GetAllDiagnostics()
        {
            return new EnabledDiagnosticOptions(s_diagnosticSets, new OrganizeUsingsSet(isRemoveUnusedImportEnabled: true, isSortImportsEnabled: true));
        }
    }
}
