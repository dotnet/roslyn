// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Provides file/project code model for a given project context.
    /// </summary>
    internal interface ICodeModelFactory
    {
        EnvDTE.FileCodeModel GetFileCodeModel(IWorkspaceProjectContext context, EnvDTE.ProjectItem item);
        EnvDTE.CodeModel GetCodeModel(IWorkspaceProjectContext context, EnvDTE.Project project);
    }
}
