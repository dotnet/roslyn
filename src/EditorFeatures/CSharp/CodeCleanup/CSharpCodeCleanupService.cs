// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeCleanup
{
    [ExportLanguageService(typeof(ICodeCleanupService), LanguageNames.CSharp), Shared]
    internal class CodeCleanupService : ICodeCleanupService
    {
        /// <summary>
        /// Maps format document code cleanup options to DiagnosticId[]
        /// </summary>
        private static ImmutableDictionary<PerLanguageOption<bool>, ImmutableArray<string>> _optionDiagnosticsMappings = GetCodeCleanupOptionMapping();

        private readonly ICodeFixService _codeFixService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeCleanupService(
            ICodeFixService codeFixService)
        {
            _codeFixService = codeFixService;
        }

        private static ImmutableDictionary<PerLanguageOption<bool>, ImmutableArray<string>> GetCodeCleanupOptionMapping()
        {
            var dictionary = new Dictionary<PerLanguageOption<bool>, ImmutableArray<string>>()
            {
                {
                    FeatureOnOffOptions.FixImplicitExplicitType,
                    ImmutableArray.Create(IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                                          IDEDiagnosticIds.UseExplicitTypeDiagnosticId)
                },
                {
                    FeatureOnOffOptions.FixThisQualification,
                    ImmutableArray.Create(IDEDiagnosticIds.AddQualificationDiagnosticId,
                                          IDEDiagnosticIds.RemoveQualificationDiagnosticId)
                },
                {
                    FeatureOnOffOptions.FixFrameworkTypes,
                    ImmutableArray.Create(IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId,
                                          IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId)
                },
                {
                    FeatureOnOffOptions.FixAddRemoveBraces,
                    ImmutableArray.Create(IDEDiagnosticIds.AddBracesDiagnosticId)
                },
                {
                    FeatureOnOffOptions.FixAccessibilityModifiers,
                    ImmutableArray.Create(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)
                },
                {
                    FeatureOnOffOptions.SortAccessibilityModifiers,
                    ImmutableArray.Create(IDEDiagnosticIds.OrderModifiersDiagnosticId)
                },
                {
                    FeatureOnOffOptions.MakeReadonly,
                    ImmutableArray.Create(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)
                },
                {
                    FeatureOnOffOptions.RemoveUnnecessaryCasts,
                    ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)
                },
                {
                    FeatureOnOffOptions.FixExpressionBodiedMembers,
                    ImmutableArray.Create(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId)
                },
                {
                    FeatureOnOffOptions.FixInlineVariableDeclarations,
                    ImmutableArray.Create(IDEDiagnosticIds.InlineDeclarationDiagnosticId)
                },
                {
                    FeatureOnOffOptions.RemoveUnusedVariables,
                    ImmutableArray.Create(IDEDiagnosticIds.DeclaredVariableNeverUsedDiagnosticId,
                                          IDEDiagnosticIds.AssignedVariableNeverUsedDiagnosticId)
                },
                {
                    FeatureOnOffOptions.FixObjectCollectionInitialization,
                    ImmutableArray.Create(IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
                                          IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)
                }
            };

            return dictionary.ToImmutableDictionary();
        }

        public async Task<Document> CleanupDocument(Document document, CancellationToken cancellationToken)
        {
            var docOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            document = await ApplyCodeFixes(document, docOptions, cancellationToken).ConfigureAwait(false);
            // do the remove usings after code fix, as code fix might remove some code which can results in unused usings.
            document = await RemoveSortUsings(document, docOptions, cancellationToken).ConfigureAwait(false);

            return document;
        }

        private async Task<Document> RemoveSortUsings(Document document, DocumentOptionSet docOptions, CancellationToken cancellationToken)
        {
            // remove usings
            if (docOptions.GetOption(FeatureOnOffOptions.RemoveUnusedUsings))
            {
                var removeUsingsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                if (removeUsingsService != null)
                {
                    document = await removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }

            // sort usings
            if (docOptions.GetOption(FeatureOnOffOptions.SortUsings))
            {
                document = await OrganizeImportsService.OrganizeImportsAsync(document, cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixes(Document document, DocumentOptionSet docOptions, CancellationToken cancellationToken)
        {
            var fixAllService = document.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            var dummy = new ProgressTracker();
            foreach (var diagnosticId in GetEnabledDiagnosticIds(docOptions))
            {
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var textSpan = new TextSpan(0, syntaxTree.Length);

                var fixCollection = await _codeFixService.GetFixesAsync(document, textSpan, diagnosticId, cancellationToken).ConfigureAwait(false);
                if (fixCollection == null)
                {
                    continue;
                }

                var fixAll = fixCollection.FixAllState;
                var solution = await fixAllService.GetFixAllChangedSolutionAsync(
                    fixAll.CreateFixAllContext(dummy, cancellationToken)).ConfigureAwait(false);

                document = solution.GetDocument(document.Id);
            }

            return document;
        }

        private List<string> GetEnabledDiagnosticIds(DocumentOptionSet docOptions)
        {
            var diagnosticIds = new List<string>();

            foreach (var featureOption in _optionDiagnosticsMappings.Keys)
            {
                if (docOptions.GetOption(featureOption))
                {
                    diagnosticIds.AddRange(_optionDiagnosticsMappings[featureOption]);
                }
            }

            return diagnosticIds;
        }
    }
}
