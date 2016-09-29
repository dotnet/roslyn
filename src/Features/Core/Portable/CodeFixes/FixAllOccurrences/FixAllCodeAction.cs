// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Fix all occurrences code action.
    /// </summary>
    internal partial class FixAllCodeAction : CodeAction
    {
        private readonly FixAllState _fixAllState;
        private readonly bool _showPreviewChangesDialog;
        private static readonly HashSet<string> s_predefinedCodeFixProviderNames = GetPredefinedCodeFixProviderNames();

        internal FixAllCodeAction(
            FixAllState fixAllState, bool showPreviewChangesDialog)
        {
            _fixAllState = fixAllState;
            _showPreviewChangesDialog = showPreviewChangesDialog;
        }

        public override string Title
        {
            get
            {
                switch (_fixAllState.Scope)
                {
                    case FixAllScope.Document:
                        return FeaturesResources.Document;
                    case FixAllScope.Project:
                        return FeaturesResources.Project;
                    case FixAllScope.Solution:
                        return FeaturesResources.Solution;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        internal override string Message => FeaturesResources.Computing_fix_all_occurrences_code_fix;

        public FixAllState FixAllState => _fixAllState;

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            return ComputeOperationsAsync(new ProgressTracker(), cancellationToken);
        }

        internal override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FixAllLogger.LogState(_fixAllState, IsInternalCodeFixProvider(_fixAllState.CodeFixProvider));

            var service = _fixAllState.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            // Use the new cancellation token instead of the stale one present inside _fixAllContext.
            return await service.GetFixAllOperationsAsync(
                _fixAllState.CreateFixAllContext(progressTracker, cancellationToken),
                _showPreviewChangesDialog).ConfigureAwait(false);
        }

        internal async override Task<Solution> GetChangedSolutionAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FixAllLogger.LogState(_fixAllState, IsInternalCodeFixProvider(_fixAllState.CodeFixProvider));

            var service = _fixAllState.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            // Use the new cancellation token instead of the stale one present inside _fixAllContext.
            return await service.GetFixAllChangedSolutionAsync(
                _fixAllState.CreateFixAllContext(progressTracker, cancellationToken)).ConfigureAwait(false);
        }

        private static bool IsInternalCodeFixProvider(CodeFixProvider fixer)
        {
            var exportAttributes = fixer.GetType().GetTypeInfo().GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), false);
            if (exportAttributes?.Any() == true)
            {
                var exportAttribute = (ExportCodeFixProviderAttribute)exportAttributes.First();
                return s_predefinedCodeFixProviderNames.Contains(exportAttribute.Name);
            }

            return false;
        }

        private static HashSet<string> GetPredefinedCodeFixProviderNames()
        {
            var names = new HashSet<string>();

            var fields = typeof(PredefinedCodeFixProviderNames).GetTypeInfo().DeclaredFields;
            foreach (var field in fields)
            {
                if (field.IsStatic)
                {
                    names.Add((string)field.GetValue(null));
                }
            }

            return names;
        }
    }
}
