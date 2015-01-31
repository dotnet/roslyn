// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal abstract class AbstractProjectCodeModel
    {
        private readonly NonReentrantLock _guard = new NonReentrantLock();
        private readonly IServiceProvider _serviceProvider;
        private readonly AbstractProject _vsProject;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;

        private CodeModelProjectCache _codeModelCache;

        public AbstractProjectCodeModel(AbstractProject project, VisualStudioWorkspace visualStudioWorkspace, IServiceProvider serviceProvider)
        {
            _vsProject = project;
            _visualStudioWorkspace = visualStudioWorkspace;
            _serviceProvider = serviceProvider;
        }

        internal void OnProjectClosed()
        {
            _codeModelCache?.OnProjectClosed();
        }

        internal CodeModelProjectCache GetCodeModelCache()
        {
            Contract.ThrowIfNull(_vsProject);
            Contract.ThrowIfNull(_visualStudioWorkspace);

            using (_guard.DisposableWait())
            {
                if (_codeModelCache == null)
                {
                    var project = _visualStudioWorkspace.CurrentSolution.GetProject(_vsProject.Id);
                    if (project == null && !_vsProject.PushingChangesToWorkspaceHosts)
                    {
                        // if this project hasn't been pushed yet, push it now so that the user gets a useful experience here.
                        _vsProject.StartPushingToWorkspaceAndNotifyOfOpenDocuments();

                        // re-check to see whether we now has the project in the workspace
                        project = _visualStudioWorkspace.CurrentSolution.GetProject(_vsProject.Id);
                    }

                    if (project != null)
                    {
                        _codeModelCache = new CodeModelProjectCache(_vsProject, _serviceProvider, project.LanguageServices, _visualStudioWorkspace);
                    }
                }

                return _codeModelCache;
            }
        }

        public IEnumerable<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>> GetFileCodeModelInstances()
        {
            return GetCodeModelCache().GetFileCodeModelInstances();
        }

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? GetFileCodeModelInstance(string fileName)
        {
            return GetCodeModelCache().GetComHandleForFileCodeModel(fileName);
        }

        internal abstract bool CanCreateFileCodeModelThroughProject(string fileName);
        internal abstract object CreateFileCodeModelThroughProject(string fileName);
    }
}
