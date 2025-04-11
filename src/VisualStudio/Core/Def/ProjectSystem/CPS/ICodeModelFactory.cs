// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem;

/// <summary>
/// Provides file/project code model for a given project context.
/// </summary>
internal interface ICodeModelFactory
{
    EnvDTE.FileCodeModel GetFileCodeModel(IWorkspaceProjectContext context, EnvDTE.ProjectItem item);
    EnvDTE.CodeModel GetCodeModel(IWorkspaceProjectContext context, EnvDTE.Project project);
}
