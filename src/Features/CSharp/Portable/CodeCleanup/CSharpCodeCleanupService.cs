// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup
{
    [ExportLanguageService(typeof(ICodeCleanupService), LanguageNames.CSharp), Shared]
    internal class CSharpCodeCleanupService : ICodeCleanupService
    {
        /// <summary>
        /// Maps format document code cleanup options to DiagnosticId[]
        /// </summary>
        private static ImmutableDictionary<PerLanguageOption<bool>, ImmutableArray<string>> _optionDiagnosticsMappings = GetCodeCleanupOptionMapping();

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

        private static ImmutableDictionary<PerLanguageOption<bool>, ImmutableArray<string>> GetCodeCleanupOptionMapping()
        {
            var dictionary = new Dictionary<PerLanguageOption<bool>, ImmutableArray<string>>()
            {
                {
                    CodeCleanupOptions.FixImplicitExplicitType,
                    ImmutableArray.Create(IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                                          IDEDiagnosticIds.UseExplicitTypeDiagnosticId)
                },
                {
                    CodeCleanupOptions.FixThisQualification,
                    ImmutableArray.Create(IDEDiagnosticIds.AddQualificationDiagnosticId,
                                          IDEDiagnosticIds.RemoveQualificationDiagnosticId)
                },
                {
                    CodeCleanupOptions.FixFrameworkTypes,
                    ImmutableArray.Create(IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId,
                                          IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId)
                },
                {
                    CodeCleanupOptions.FixAddRemoveBraces,
                    ImmutableArray.Create(IDEDiagnosticIds.AddBracesDiagnosticId)
                },
                {
                    CodeCleanupOptions.FixAccessibilityModifiers,
                    ImmutableArray.Create(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)
                },
                {
                    CodeCleanupOptions.SortAccessibilityModifiers,
                    ImmutableArray.Create(IDEDiagnosticIds.OrderModifiersDiagnosticId)
                },
                {
                    CodeCleanupOptions.MakeReadonly,
                    ImmutableArray.Create(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)
                },
                {
                    CodeCleanupOptions.RemoveUnnecessaryCasts,
                    ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)
                },
                {
                    CodeCleanupOptions.FixExpressionBodiedMembers,
                    ImmutableArray.Create(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                                          IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId)
                },
                {
                    CodeCleanupOptions.FixInlineVariableDeclarations,
                    ImmutableArray.Create(IDEDiagnosticIds.InlineDeclarationDiagnosticId)
                },
                {
                    CodeCleanupOptions.RemoveUnusedVariables,
                    ImmutableArray.Create(CSharpRemoveUnusedVariableCodeFixProvider.CS0168,
                                          CSharpRemoveUnusedVariableCodeFixProvider.CS0219)
                },
                {
                    CodeCleanupOptions.FixObjectCollectionInitialization,
                    ImmutableArray.Create(IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
                                          IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)
                }
            };

            return dictionary.ToImmutableDictionary();
        }

        public async Task<Document> CleanupAsync(Document document, CancellationToken cancellationToken)
        {
            var docOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            if (_codeFixServiceOpt != null)
            {
                document = await ApplyCodeFixesAsync(document, docOptions, cancellationToken).ConfigureAwait(false);
            }

            // do the remove usings after code fix, as code fix might remove some code which can results in unused usings.
            document = await RemoveSortUsingsAsync(document, docOptions, cancellationToken).ConfigureAwait(false);

            return await Formatter.FormatAsync(document).ConfigureAwait(false);
        }

        private async Task<Document> RemoveSortUsingsAsync(Document document, DocumentOptionSet docOptions, CancellationToken cancellationToken)
        {
            // remove usings
            if (docOptions.GetOption(CodeCleanupOptions.RemoveUnusedImports))
            {
                var removeUsingsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                if (removeUsingsService != null)
                {
                    document = await removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }

            // sort usings
            if (docOptions.GetOption(CodeCleanupOptions.SortImports))
            {
                document = await OrganizeImportsService.OrganizeImportsAsync(document, cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesAsync(Document document, DocumentOptionSet docOptions, CancellationToken cancellationToken)
        {
            var fixAllService = document.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            var dummy = new ProgressTracker();
            foreach (var diagnosticId in GetEnabledDiagnosticIds(docOptions))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var textSpan = new TextSpan(0, syntaxTree.Length);

                var fixCollection = await _codeFixServiceOpt.GetFixesAsync(document, textSpan, diagnosticId, cancellationToken).ConfigureAwait(false);
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

        private IEnumerable<string> GetEnabledDiagnosticIds(DocumentOptionSet docOptions)
        {
            var diagnosticIds = new List<string>();

            foreach (var kv in _optionDiagnosticsMappings)
            {
                if (docOptions.GetOption(kv.Key))
                {
                    diagnosticIds.AddRange(kv.Value);
                }
            }

            return diagnosticIds;
        }
    }
}
