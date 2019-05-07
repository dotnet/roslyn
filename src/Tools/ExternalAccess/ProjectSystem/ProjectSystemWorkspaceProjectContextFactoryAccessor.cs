// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    [Export(typeof(IProjectSystemWorkspaceProjectContextFactoryAccessor))]
    [Shared]
    internal sealed class ProjectSystemWorkspaceProjectContextFactoryAccessor : IProjectSystemWorkspaceProjectContextFactoryAccessor
    {
        private readonly IWorkspaceProjectContextFactory _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectSystemWorkspaceProjectContextFactoryAccessor(IWorkspaceProjectContextFactory implementation)
        {
            _implementation = implementation;
        }

        public ProjectSystemWorkspaceProjectContextWrapper CreateProjectContext(string languageName, string projectUniqueName, string projectFilePath, Guid projectGuid, object hierarchy, string binOutputPath)
            => new ProjectSystemWorkspaceProjectContextWrapper(_implementation.CreateProjectContext(languageName, projectUniqueName, projectFilePath, projectGuid, hierarchy, binOutputPath));
    }
}
