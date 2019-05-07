// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    internal interface IProjectSystemCodeModelFactoryAccessor
    {
        EnvDTE.FileCodeModel GetFileCodeModel(ProjectSystemWorkspaceProjectContextWrapper context, EnvDTE.ProjectItem item);
        EnvDTE.CodeModel GetCodeModel(ProjectSystemWorkspaceProjectContextWrapper context, EnvDTE.Project project);
    }
}
