// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeCleanup
{
    [Export(typeof(ICodeCleanupService))]
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

        public Task<IEnumerable<TextChange>> GetChangesForCleanupDocument(Document document, CancellationToken cancellationToken)
        {
            var oldDocument = document;
            document = ApplyCodeFixes(document, cancellationToken);
            // do the remove usings after code fix, as code fix might remove some code which can results in unused usings.
            document = RemoveSortUsings(document, cancellationToken);

            return document.GetTextChangesAsync(oldDocument, cancellationToken);
        }

        private Document RemoveSortUsings(Document document, CancellationToken cancellationToken)
        {
            // remove usings
            if (document.Project.Solution.Workspace.Options.GetOption(FeatureOnOffOptions.RemoveUnusedUsings, LanguageNames.CSharp))
            {
                var removeUsingsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                if (removeUsingsService != null)
                {
                    document = removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                }
            }

            // sort usings
            if (document.Project.Solution.Workspace.Options.GetOption(FeatureOnOffOptions.SortUsings, LanguageNames.CSharp))
            {
                document = OrganizeImportsService.OrganizeImportsAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
            }

            return document;
        }

        private Document ApplyCodeFixes(Document document, CancellationToken cancellationToken)
        {
            var fixAllService = document.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            var dummy = new ProgressTracker();
            foreach (var diagnosticId in GetEnabledDiagnosticIds(document.Project.Solution.Workspace))
            {
                var length = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken).Length;
                var textSpan = new TextSpan(0, length);

                var fixCollection = _codeFixService.GetFixesAsync(document, textSpan, diagnosticId, cancellationToken).WaitAndGetResult(cancellationToken);
                if (fixCollection == null)
                {
                    continue;
                }

                var fixAll = fixCollection.FixAllState;
                var solution = fixAllService.GetFixAllChangedSolutionAsync(fixAll.CreateFixAllContext(dummy, cancellationToken)).WaitAndGetResult(cancellationToken);
                document = solution.GetDocument(document.Id);
            }

            return document;
        }

        private List<string> GetEnabledDiagnosticIds(Workspace workspace)
        {
            var diagnosticIds = new List<string>();

            foreach (var featureOption in _optionDiagnosticsMappings.Keys)
            {
                if (workspace.Options.GetOption(featureOption, LanguageNames.CSharp))
                {
                    diagnosticIds.AddRange(_optionDiagnosticsMappings[featureOption]);
                }
            }

            return diagnosticIds;
        }
    }
}
