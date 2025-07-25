// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static partial class ITextBufferExtensions
{
    extension(ITextBuffer buffer)
    {
        internal bool IsInLspEditorContext()
        {
            if (buffer.TryGetWorkspace(out var workspace))
            {
                var workspaceContextService = workspace.Services.GetRequiredService<IWorkspaceContextService>();
                return workspaceContextService.IsInLspEditorContext();
            }

            return false;
        }

        internal bool TryGetWorkspace([NotNullWhen(true)] out Workspace? workspace)
            => Workspace.TryGetWorkspace(buffer.AsTextContainer(), out workspace);

        /// <summary>
        /// Checks if a buffer supports refactorings.
        /// </summary>
        internal bool SupportsRefactorings()
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsRefactorings(buffer);

        /// <summary>
        /// Checks if a buffer supports rename.
        /// </summary>
        internal bool SupportsRename()
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsRename(buffer);

        /// <summary>
        /// Checks if a buffer supports code fixes.
        /// </summary>
        internal bool SupportsCodeFixes()
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsCodeFixes(buffer);

        /// <summary>
        /// Checks if a buffer supports navigation.
        /// </summary>
        internal bool SupportsNavigationToAnyPosition()
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsNavigationToAnyPosition(buffer);

        public ITextSnapshot ApplyChange(TextChange change)
        {
            if (buffer.Properties.TryGetProperty<IContainedDocument>(typeof(IContainedDocument), out var containedDocument))
            {
                return containedDocument.ApplyChanges([change]);
            }

            using var edit = buffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null);
            edit.Replace(change.Span.ToSpan(), change.NewText);
            return edit.Apply();
        }

        public ITextSnapshot ApplyChanges(IEnumerable<TextChange> changes)
        {
            if (buffer.Properties.TryGetProperty<IContainedDocument>(typeof(IContainedDocument), out var containedDocument))
            {
                return containedDocument.ApplyChanges(changes);
            }

            using var edit = buffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null);
            foreach (var change in changes)
            {
                edit.Replace(change.Span.ToSpan(), change.NewText);
            }

            return edit.Apply();
        }
    }

    private static bool TryGetSupportsFeatureService(ITextBuffer buffer, [NotNullWhen(true)] out ITextBufferSupportsFeatureService? service)
    {
        service = null;
        if (buffer.TryGetWorkspace(out var workspace))
        {
            service = workspace.Services.GetService<ITextBufferSupportsFeatureService>();
        }

        return service != null;
    }
}
