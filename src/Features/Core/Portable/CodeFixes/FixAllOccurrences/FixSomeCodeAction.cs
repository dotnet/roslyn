// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal abstract class FixSomeCodeAction : CodeAction
    {
        private static readonly HashSet<string> s_predefinedCodeFixProviderNames = GetPredefinedCodeFixProviderNames();

        internal readonly FixAllState FixAllState;
        private bool _showPreviewChangesDialog;

        internal FixSomeCodeAction(
            FixAllState fixAllState, bool showPreviewChangesDialog)
        {
            FixAllState = fixAllState;
            _showPreviewChangesDialog = showPreviewChangesDialog;
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => await ComputeOperationsAsync(new ProgressTracker(), cancellationToken).ConfigureAwait(false);

        internal override Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FixAllLogger.LogState(FixAllState, IsInternalCodeFixProvider(FixAllState.CodeFixProvider));

            var service = FixAllState.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            // Use the new cancellation token instead of the stale one present inside _fixAllContext.
            return service.GetFixAllOperationsAsync(
                new FixAllContext(FixAllState, progressTracker, cancellationToken),
                _showPreviewChangesDialog);
        }

        internal override async Task<Solution> GetChangedSolutionAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FixAllLogger.LogState(FixAllState, IsInternalCodeFixProvider(FixAllState.CodeFixProvider));

            var service = FixAllState.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();

            // Use the new cancellation token instead of the stale one present inside _fixAllContext.
            return await service.GetFixAllChangedSolutionAsync(
                new FixAllContext(FixAllState, progressTracker, cancellationToken)).ConfigureAwait(false);
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

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly FixSomeCodeAction _fixSomeCodeAction;

            internal TestAccessor(FixSomeCodeAction fixSomeCodeAction)
                => _fixSomeCodeAction = fixSomeCodeAction;

            /// <summary>
            /// Gets a reference to <see cref="_showPreviewChangesDialog"/>, which can be read or written by test code.
            /// </summary>
            public ref bool ShowPreviewChangesDialog
                => ref _fixSomeCodeAction._showPreviewChangesDialog;
        }
    }
}
