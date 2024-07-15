// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

internal interface IProjectCodeModel
{
    EnvDTE.FileCodeModel GetOrCreateFileCodeModel(string filePath, object? parent);
    EnvDTE.FileCodeModel CreateFileCodeModel(CodeAnalysis.SourceGeneratedDocument sourceGeneratedDocument);
    EnvDTE.CodeModel GetOrCreateRootCodeModel(Project parent);
    void OnSourceFileRemoved(string fileName);
    void OnSourceFileRenaming(string filePath, string newFilePath);
    void OnProjectClosed();
}
