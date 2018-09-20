// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.CodeCleanup;
using Microsoft.VisualStudio.Language.CodeCleanUp;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    internal class CSharpCodeCleanupFixer : ForegroundThreadAffinitizedObject, ICodeCleanUpFixer
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
        
        public CSharpCodeCleanupFixer(ICodeFixService codeFixService, IThreadingContext threadingContext, bool assertIsForeground = false)
            : base(threadingContext, assertIsForeground)
        {
            _codeFixServiceOpt = codeFixService;
        }

        public async Task<bool> FixAsync(ICodeCleanUpScope scope, FixIdContainer enabledFixIds, CancellationToken cancellationToken)
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
                    var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    await CleanupDocument(document, cancellationToken).ConfigureAwait(false);
                }
            }
            return false;
        }

        private async Task<bool> CleanupDocument(Document document, CancellationToken cancellationToken)
        {
            var oldDoc = document;
            
            foreach (var errorCode in _errorCodes)
            {
                var changedDoc = await _codeFixServiceOpt.ApplyCodeFixesForSpecificDiagnosticId(document, errorCode, cancellationToken).ConfigureAwait(true);
                if (changedDoc != null)
                {
                    document = changedDoc;                    
                }
            }

            document = await RemoveAndSortUsingsAsync(document, cancellationToken).ConfigureAwait(true);
            
            var codeCleanupChanges = await document.GetTextChangesAsync(oldDoc, cancellationToken).ConfigureAwait(false);
            if (codeCleanupChanges != null && codeCleanupChanges.Any())
            {
                //progressTracker.Description = EditorFeaturesResources.Applying_changes; 
                using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
                {
                    document.Project.Solution.Workspace.ApplyTextChanges(document.Id, codeCleanupChanges, cancellationToken);
                }

                return true;
            }

            return false;
        }


        private async Task<Document> RemoveAndSortUsingsAsync(Document document, CancellationToken cancellationToken)
        {
            // removing unused usings:
            if (_errorCodes.Contains(RemoveUnusedImports))
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

            // sort usings:
            if (_errorCodes.Contains(SortImports))
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_SortImports, cancellationToken))
                {
                    document = await OrganizeImportsService.OrganizeImportsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }
    }
}
