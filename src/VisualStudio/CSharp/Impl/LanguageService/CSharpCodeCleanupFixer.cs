// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.CodeCleanup;
using Microsoft.VisualStudio.Language.CodeCleanUp;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    internal class CSharpCodeCleanupFixer : ForegroundThreadAffinitizedObject, ICodeCleanUpFixer
    {
        private readonly ICodeFixService _codeFixServiceOpt;
        /// <summary>
        /// TODO: hardcoded list need to be replace by inclusion/exclusion list from .editorconfig
        /// </summary>
        private ImmutableArray<string> _errorCodes = ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId,
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
                var itemId = hierarchyContent.ItemId;

                if (hierarchy.GetCanonicalName(itemId, out var path) == 0)
                {
                    
                }

            }
            else
            {
                var buffer = textBufferScope.SubjectBuffer;
                if (buffer != null)
                {
                    var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    var oldDoc = document;

                    var isFixed = false;
                    foreach (var errorCode in _errorCodes)
                    {
                        var changedDoc = await _codeFixServiceOpt.ApplyCodeFixesForSpecificDiagnosticId(document, errorCode, cancellationToken).ConfigureAwait(true);
                        if (changedDoc != null)
                        {
                            document = changedDoc;
                            isFixed = true;
                        }
                    }

                    if (isFixed)
                    {
                        var codeCleanupChanges = await document.GetTextChangesAsync(oldDoc, cancellationToken).ConfigureAwait(false);
                        if (codeCleanupChanges != null && codeCleanupChanges.Any())
                        {
                            //progressTracker.Description = EditorFeaturesResources.Applying_changes; 
                            using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
                            {
                                document.Project.Solution.Workspace.ApplyTextChanges(document.Id, codeCleanupChanges, cancellationToken);
                            }
                        }

                    }

                    return isFixed;
                }
            }
            return false;
        }         
    }
}
