// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// The root type that is held by a project to provide CodeModel support.
    /// </summary>
    internal sealed class ProjectCodeModel
    {
        private readonly NonReentrantLock _guard = new NonReentrantLock();
        private readonly ProjectId _projectId;
        private readonly ICodeModelInstanceFactory _codeModelInstanceFactory;
        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspace;
        private readonly IServiceProvider _serviceProvider;

        private CodeModelProjectCache _codeModelCache;

        public ProjectCodeModel(ProjectId projectId, ICodeModelInstanceFactory codeModelInstanceFactory, VisualStudioWorkspaceImpl visualStudioWorkspace, IServiceProvider serviceProvider)
        {
            _projectId = projectId;
            _codeModelInstanceFactory = codeModelInstanceFactory;
            _visualStudioWorkspace = visualStudioWorkspace;
            _serviceProvider = serviceProvider;
        }

        internal void OnProjectClosed()
        {
            _codeModelCache?.OnProjectClosed();
        }

        private CodeModelProjectCache GetCodeModelCache()
        {
            Contract.ThrowIfNull(_projectId);
            Contract.ThrowIfNull(_visualStudioWorkspace);

            using (_guard.DisposableWait())
            {
                if (_codeModelCache == null)
                {
                    var workspaceProject = _visualStudioWorkspace.CurrentSolution.GetProject(_projectId);
                    var hostProject = _visualStudioWorkspace.GetHostProject(_projectId);
                    if (workspaceProject == null && !hostProject.PushingChangesToWorkspace)
                    {
                        // if this project hasn't been pushed yet, push it now so that the user gets a useful experience here.
                        hostProject.StartPushingToWorkspaceAndNotifyOfOpenDocuments();

                        // re-check to see whether we now has the project in the workspace
                        workspaceProject = _visualStudioWorkspace.CurrentSolution.GetProject(_projectId);
                    }

                    if (workspaceProject != null)
                    {
                        _codeModelCache = new CodeModelProjectCache(_projectId, _codeModelInstanceFactory, _serviceProvider, workspaceProject.LanguageServices, _visualStudioWorkspace);
                    }
                }

                return _codeModelCache;
            }
        }

        public IEnumerable<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>> GetCachedFileCodeModelInstances()
        {
            return GetCodeModelCache().GetFileCodeModelInstances();
        }

        public bool TryGetCachedFileCodeModel(string fileName, out ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> fileCodeModelHandle)
        {
            var handle = GetCodeModelCache()?.GetComHandleForFileCodeModel(fileName);

            fileCodeModelHandle = handle != null
                ? handle.Value
                : default(ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>);

            return handle != null;
        }

        /// <summary>
        /// Gets or creates a <see cref="FileCodeModel"/> for the given file name. Because we don't have
        /// a parent object, this will call back to the project system to provide us the parent object.
        /// </summary>
        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> GetOrCreateFileCodeModel(string filePath)
        {
            return GetCodeModelCache().GetOrCreateFileCodeModel(filePath);
        }

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> GetOrCreateFileCodeModel(string filePath, object parent)
        {
            return GetCodeModelCache().GetOrCreateFileCodeModel(filePath, parent);
        }

        public EnvDTE.CodeModel GetOrCreateRootCodeModel(EnvDTE.Project parent)
        {
            return GetCodeModelCache().GetOrCreateRootCodeModel(parent);
        }

        public void OnSourceFileRemoved(string fileName)
        {
            GetCodeModelCache().OnSourceFileRemoved(fileName);
        }

        public void OnSourceFileRenaming(string filePath, string newFilePath)
        {
            GetCodeModelCache().OnSourceFileRenaming(filePath, newFilePath);
        }
    }
}
