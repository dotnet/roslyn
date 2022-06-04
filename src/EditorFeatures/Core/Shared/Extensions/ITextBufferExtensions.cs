// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ITextBufferExtensions
    {
        internal static bool IsInLspEditorContext(this ITextBuffer buffer)
        {
            if (buffer.TryGetWorkspace(out var workspace))
            {
                var workspaceContextService = workspace.Services.GetRequiredService<IWorkspaceContextService>();
                return workspaceContextService.IsInLspEditorContext();
            }

            return false;
        }

        internal static bool TryGetWorkspace(this ITextBuffer buffer, [NotNullWhen(true)] out Workspace? workspace)
            => Workspace.TryGetWorkspace(buffer.AsTextContainer(), out workspace);

        /// <summary>
        /// Checks if a buffer supports refactorings.
        /// </summary>
        internal static bool SupportsRefactorings(this ITextBuffer buffer)
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsRefactorings(buffer);

        /// <summary>
        /// Checks if a buffer supports rename.
        /// </summary>
        internal static bool SupportsRename(this ITextBuffer buffer)
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsRename(buffer);

        /// <summary>
        /// Checks if a buffer supports code fixes.
        /// </summary>
        internal static bool SupportsCodeFixes(this ITextBuffer buffer)
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsCodeFixes(buffer);

        /// <summary>
        /// Checks if a buffer supports navigation.
        /// </summary>
        internal static bool SupportsNavigationToAnyPosition(this ITextBuffer buffer)
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsNavigationToAnyPosition(buffer);

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
}
