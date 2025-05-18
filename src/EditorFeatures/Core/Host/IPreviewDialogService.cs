// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Host;

/// <summary>
/// Displays the Preview Changes Dialog comparing two solutions.
/// </summary>
internal interface IPreviewDialogService : IWorkspaceService
{
    /// <summary>
    /// Presents the user a preview of the changes, based on a textual diff
    /// between <paramref name="newSolution"/> and <paramref name="oldSolution"/>.
    /// </summary>
    /// <param name="title">The title of the preview changes dialog.</param>
    /// <param name="helpString">The keyword used by F1 help in the dialog.</param>
    /// <param name="description">Text to display above the treeview in the dialog.</param>
    /// <param name="topLevelName">The name of the root item in the treeview in the dialog.</param>
    /// <param name="topLevelGlyph">The <see cref="Glyph"/> of the root item in the treeview.</param>
    /// <param name="newSolution">The changes to preview.</param>
    /// <param name="oldSolution">The baseline solution.</param>
    /// <param name="showCheckBoxes">Whether or not preview dialog should display item checkboxes.</param>
    /// <returns>Returns <paramref name="oldSolution"/> with the changes selected in the dialog
    /// applied. Returns null if cancelled.</returns>
    Solution PreviewChanges(
        string title,
        string helpString,
        string description,
        string topLevelName,
        Glyph topLevelGlyph,
        Solution newSolution,
        Solution oldSolution,
        bool showCheckBoxes = true);
}
