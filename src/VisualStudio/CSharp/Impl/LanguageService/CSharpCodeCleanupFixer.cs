// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.CodeCleanup;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [Export(typeof(CodeCleanUpFixer))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.CSharpContentType)]
    internal class CSharpCodeCleanUpFixer : CodeCleanUpFixer
    {
        public override Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {
            switch(scope)
            {
                case TextBufferCodeCleanUpScope textBufferScope:
                    return FixTextBufferAsync(textBufferScope, context, cancellationToken);
                case IVsHierarchyCodeCleanupScope hierarchyContentScope:
                    return FixHierarchyContentAsync(hierarchyContentScope, context, cancellationToken);
                default:
                    return Task.FromResult(false);
            }            
        }

        private Task<bool> FixHierarchyContentAsync(IVsHierarchyCodeCleanupScope hierarchyContent, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {            
            // TODO: this one will be implemented later
            var hierarchy = hierarchyContent.Hierarchy;
            if (hierarchy == null)
            {
                // solution
                return Task.FromResult(false);
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

            return Task.FromResult(false);
        }

        private async Task<bool> FixTextBufferAsync(TextBufferCodeCleanUpScope textBufferScope, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(context.OperationContext.UserCancellationToken, cancellationToken).Token;

            var buffer = textBufferScope.SubjectBuffer;
            if (buffer != null)
            {
                using (var scope = context.OperationContext.AddScope(allowCancellation: true, description: EditorFeaturesResources.Applying_changes))
                {
                    var progressTracker = new ProgressTracker((description, completed, total) =>
                    {
                        if (scope != null)
                        {
                            scope.Description = description;
                            scope?.Progress.Report(new ProgressInfo(completed, total));
                        }
                    });

                    var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

                    // TODO: enable all diagnostics for now, need to be replace by inclusion/ exclusion list from .editorconfig
                    var enabledDiagnostics = codeCleanupService.GetAllDiagnostics();

                    var newDoc = await codeCleanupService.CleanupAsync(
                        document, enabledDiagnostics, progressTracker, cancellationToken);

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
