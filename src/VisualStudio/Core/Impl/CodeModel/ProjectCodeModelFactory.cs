// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    [Export(typeof(IProjectCodeModelFactory))]
    [Export(typeof(ProjectCodeModelFactory))]
    internal sealed class ProjectCodeModelFactory : IProjectCodeModelFactory
    {
        private readonly ConcurrentDictionary<ProjectId, ProjectCodeModel> _projectCodeModels = new ConcurrentDictionary<ProjectId, ProjectCodeModel>();

        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly IServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public ProjectCodeModelFactory(VisualStudioWorkspace visualStudioWorkspace, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IThreadingContext threadingContext)
        {
            _visualStudioWorkspace = visualStudioWorkspace;
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;
        }

        public IProjectCodeModel CreateProjectCodeModel(ProjectId id, ICodeModelInstanceFactory codeModelInstanceFactory)
        {
            var projectCodeModel = new ProjectCodeModel(_threadingContext, id, codeModelInstanceFactory, _visualStudioWorkspace, _serviceProvider, this);
            if (!_projectCodeModels.TryAdd(id, projectCodeModel))
            {
                throw new InvalidOperationException($"A {nameof(IProjectCodeModel)} has already been created for project with ID {id}");
            }

            return projectCodeModel;
        }

        public ProjectCodeModel GetProjectCodeModel(ProjectId id)
        {
            if (!_projectCodeModels.TryGetValue(id, out var projectCodeModel))
            {
                throw new InvalidOperationException($"No {nameof(ProjectCodeModel)} exists for project with ID {id}");
            }

            return projectCodeModel;
        }

        internal void OnProjectClosed(ProjectId projectId)
        {
            _projectCodeModels.TryRemove(projectId, out _);
        }

        public ProjectCodeModel TryGetProjectCodeModel(ProjectId id)
        {
            _projectCodeModels.TryGetValue(id, out var projectCodeModel);
            return projectCodeModel;
        }
    }
}
