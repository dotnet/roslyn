// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.CodeCleanup;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [Export(typeof(CodeCleanUpFixer))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.CSharpContentType)]
    internal class CSharpCodeCleanUpFixer : CodeCleanUpFixer
    {
        public const string RemoveUnusedImports = nameof(RemoveUnusedImports);
        public const string SortImports = nameof(SortImports);

        private readonly ICodeFixService _codeFixServiceOpt;
        /// <summary>
        /// TODO: hardcoded list need to be replace by inclusion/exclusion list from .editorconfig
        /// </summary>
        private ImmutableArray<string> _errorCodes = ImmutableArray.Create(RemoveUnusedImports,
                                                                           SortImports,
                                                                           IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                                                                           IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                                                                           IDEDiagnosticIds.AddQualificationDiagnosticId,
                                                                           IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                                                                           IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId,
                                                                           IDEDiagnosticIds.AddBracesDiagnosticId,
                                                                           IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId,
                                                                           IDEDiagnosticIds.OrderModifiersDiagnosticId,
                                                                           IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId,
                                                                           IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId,
                                                                           IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                                                                           IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                                                                           IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                                                                           IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                                                                           IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                                                                           IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                                                                           IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                                                                           IDEDiagnosticIds.InlineDeclarationDiagnosticId,
                                                                           CSharpRemoveUnusedVariableCodeFixProvider.CS0168,
                                                                           CSharpRemoveUnusedVariableCodeFixProvider.CS0219,
                                                                           IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
                                                                           IDEDiagnosticIds.UseCollectionInitializerDiagnosticId);

        [ImportingConstructor]
        public CSharpCodeCleanUpFixer(ICodeFixService codeFixService)
        {
            _codeFixServiceOpt = codeFixService;
        }

        public override async Task<bool> FixAsync(ICodeCleanUpScope scope, FixIdContainer enabledFixIds, CancellationToken cancellationToken)
        {
            var textBufferScope = scope as TextBufferCodeCleanUpScope;
            if (textBufferScope == null)
            {
                var hierarchyContent = scope as IVsHierarchyCodeCleanupScope;
                var hierarchy = hierarchyContent.Hierarchy;
                if (hierarchy == null)
                {
                    // solution
                    return false;
                }

                var itemId = hierarchyContent.ItemId;

                if (hierarchy.GetCanonicalName(itemId, out var path) == 0)
                {
                    var attr = File.GetAttributes(path);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        // directory
                    }
                    else
                    {
                        // document
                    }
                }

            }
            else
            {
                var buffer = textBufferScope.SubjectBuffer;
                if (buffer != null)
                {
                    var progressTracker = new ProgressTracker();
                    var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

                    var organizeUsingsSet = new OrganizeUsingsSet(true, true);
                    var enabledDiagnostics = codeCleanupService.GetAllDiagnostics();

                    var newDoc = await codeCleanupService.CleanupAsync(
                        document, organizeUsingsSet, enabledDiagnostics, progressTracker, cancellationToken);

                    var codeCleanupChanges = await newDoc.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
                    if (codeCleanupChanges != null && codeCleanupChanges.Any())
                    {
                        progressTracker.Description = EditorFeaturesResources.Applying_changes; 
                        using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
                        {
                            newDoc.Project.Solution.Workspace.ApplyTextChanges(newDoc.Id, codeCleanupChanges, cancellationToken);
                        }

                        return true;
                    }
                }
            }
            return false;
        }
    }
}
