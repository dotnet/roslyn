// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    [Export(typeof(IProjectSystemCodeModelFactoryAccessor))]
    [Shared]
    internal sealed class ProjectSystemCodeModelFactoryAccessor : IProjectSystemCodeModelFactoryAccessor
    {
        private readonly ICodeModelFactory _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectSystemCodeModelFactoryAccessor(ICodeModelFactory implementation)
        {
            _implementation = implementation;
        }

        public EnvDTE.CodeModel GetCodeModel(ProjectSystemWorkspaceProjectContextWrapper context, EnvDTE.Project project)
            => _implementation.GetCodeModel(context.WorkspaceProjectContext, project);

        public EnvDTE.FileCodeModel GetFileCodeModel(ProjectSystemWorkspaceProjectContextWrapper context, EnvDTE.ProjectItem item)
            => _implementation.GetFileCodeModel(context.WorkspaceProjectContext, item);
    }
}
