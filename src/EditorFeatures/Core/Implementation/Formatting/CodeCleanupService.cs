// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    [Export]
    [Export(typeof(ICodeCleanupService))]
    internal class CodeCleanupService : ICodeCleanupService
    {
        private static Lazy<IDictionary<PerLanguageOption<bool>, string[]>> _dictionary
            = new Lazy<IDictionary<PerLanguageOption<bool>, string[]>>(GetCodeCleanupOptionMapping);

        private readonly ICodeFixService _codeFixService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeCleanupService(
            ICodeFixService codeFixService)
        {
            _codeFixService = codeFixService;
        }
        public static IDictionary<PerLanguageOption<bool>, string[]> Dictionary
        {
            get
            {
                return _dictionary.Value;
            }
        }

        private static IDictionary<PerLanguageOption<bool>, string[]> GetCodeCleanupOptionMapping()
        {
            var dictionary = new Dictionary<PerLanguageOption<bool>, string[]>();
            dictionary.Add(FeatureOnOffOptions.FixImplicitExplicitType,
                new[] { IDEDiagnosticIds.UseImplicitTypeDiagnosticId, IDEDiagnosticIds.UseExplicitTypeDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.FixThisQualification,
                new[] { IDEDiagnosticIds.AddQualificationDiagnosticId, IDEDiagnosticIds.RemoveQualificationDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.FixFrameworkTypes,
                new[] { IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId, IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.FixAddRemoveBraces,
                new[] { IDEDiagnosticIds.AddBracesDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.FixAccessibilityModifiers,
                new[] { IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.SortAccessibilityModifiers,
                new[] { IDEDiagnosticIds.OrderModifiersDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.MakeReadonly,
                new[] { IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.RemoveUnnecessaryCasts,
                new[] { IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.FixExpressionBodiedMembers,
                new[] { IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.FixInlineVariableDeclarations,
                new[] { IDEDiagnosticIds.InlineDeclarationDiagnosticId });
            dictionary.Add(FeatureOnOffOptions.RemoveUnusedVariables,
                new[] { "CS0168", "CS0219" });
            dictionary.Add(FeatureOnOffOptions.FixObjectCollectionInitialization,
                new[] { IDEDiagnosticIds.UseObjectInitializerDiagnosticId, IDEDiagnosticIds.UseCollectionInitializerDiagnosticId });
            //dictionary.Add(FeatureOnOffOptions.FixLanguageFeatures,
            //    new[] { IDEDiagnosticIds. });
            //IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId,
            //    IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId,
            //    IDEDiagnosticIds.InlineAsTypeCheckId,
            //    IDEDiagnosticIds.InlineIsTypeCheckId,
            //    IDEDiagnosticIds.InlineIsTypeWithoutNameCheckDiagnosticsId,

            return dictionary;
        }

        public Document CleanupDocument(Document document, CancellationToken cancellationToken)
        {
            var oldDocument = document;
            var optionService = document.Project.Solution.Workspace.Services.GetService<IOptionService>();
            document = RemoveSortUsings(document, optionService, cancellationToken);
            document = ApplyCodeFixes(document, optionService, cancellationToken);

            var codeFixChanges = document.GetTextChangesAsync(oldDocument, cancellationToken).WaitAndGetResult(cancellationToken).ToList();

            // we should do apply changes only once. but for now, we just do it twice, for all others and formatting
            if (codeFixChanges.Count > 0)
            {
                ApplyChanges(oldDocument, codeFixChanges, selectionOpt: null, cancellationToken);
            }

            return document;
        }

        /// <summary>
        /// TODO: copied from FormatCommandHandler.cs, will refactor to a better place
        /// </summary>
        /// <param name="document"></param>
        /// <param name="changes"></param>
        /// <param name="selectionOpt"></param>
        /// <param name="cancellationToken"></param>
        private void ApplyChanges(Document document, IList<TextChange> changes, TextSpan? selectionOpt, CancellationToken cancellationToken)
        {
            if (selectionOpt.HasValue)
            {
                var ruleFactory = document.Project.Solution.Workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();

                changes = ruleFactory.FilterFormattedChanges(document, selectionOpt.Value, changes).ToList();
                if (changes.Count == 0)
                {
                    return;
                }
            }

            using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
            {
                document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, cancellationToken);
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


        private List<string> GetEnabledDiagnosticIds(IOptionService optionService)
        {
            var diagnosticIds = new List<string>();

            foreach (var featureOption in CodeCleanupService.Dictionary.Keys)
            {
                if (optionService.GetOption(featureOption, LanguageNames.CSharp))
                {
                    diagnosticIds.AddRange(CodeCleanupService.Dictionary[featureOption]);
                }
            }

            return diagnosticIds;
        }
    }
}
