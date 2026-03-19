// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

internal sealed class TextManagerAdapter : ITextManagerAdapter
{
    public EnvDTE.TextPoint CreateTextPoint(FileCodeModel fileCodeModel, VirtualTreePoint point)
    {
        if (!fileCodeModel.TryGetDocument(out var document))
        {
            return null;
        }

        var hierarchyOpt = fileCodeModel.Workspace.GetHierarchy(document.Project.Id);

        using var invisibleEditor = new InvisibleEditor(fileCodeModel.ServiceProvider, document.FilePath, hierarchyOpt, needsSave: false, needsUndoDisabled: false);
        var vsTextLines = invisibleEditor.VsTextLines;

        var line = point.GetContainingLine();
        var column = point.Position - line.Start + point.VirtualSpaces;
        Marshal.ThrowExceptionForHR(vsTextLines.CreateTextPoint(line.LineNumber, column, out var textPoint));
        return (EnvDTE.TextPoint)textPoint;
    }
}
