// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal sealed class TextManagerAdapter : ITextManagerAdapter
    {
        public EnvDTE.TextPoint CreateTextPoint(FileCodeModel fileCodeModel, VirtualTreePoint point)
        {
            var workspace = fileCodeModel.Workspace as VisualStudioWorkspaceImpl;
            var hostDocument = workspace.GetHostDocument(fileCodeModel.GetDocumentId());
            if (hostDocument == null)
            {
                return null;
            }

            using (var invisibleEditor = new InvisibleEditor(fileCodeModel.ServiceProvider, hostDocument.FilePath, hostDocument.Project, needsSave: false, needsUndoDisabled: false))
            {
                var vsTextLines = invisibleEditor.VsTextLines;

                var line = point.GetContainingLine();
                var column = point.Position - line.Start + point.VirtualSpaces;
                Marshal.ThrowExceptionForHR(vsTextLines.CreateTextPoint(line.LineNumber, column, out var textPoint));
                return (EnvDTE.TextPoint)textPoint;
            }
        }
    }
}
