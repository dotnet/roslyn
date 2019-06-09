// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// The root type that is held by a project to provide CodeModel support.
    /// </summary>
    internal sealed class ProjectCodeModel : IProjectCodeModel
    {
        private readonly NonReentrantLock _guard = new NonReentrantLock();
        private readonly IThreadingContext _threadingContext;
        private readonly ProjectId _projectId;
        private readonly ICodeModelInstanceFactory _codeModelInstanceFactory;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly IServiceProvider _serviceProvider;
        private readonly ProjectCodeModelFactory _projectCodeModelFactory;

        private CodeModelProjectCache _codeModelCache;

        public ProjectCodeModel(IThreadingContext threadingContext, ProjectId projectId, ICodeModelInstanceFactory codeModelInstanceFactory, VisualStudioWorkspace visualStudioWorkspace, IServiceProvider serviceProvider, ProjectCodeModelFactory projectCodeModelFactory)
        {
            _threadingContext = threadingContext;
            _projectId = projectId;
            _codeModelInstanceFactory = codeModelInstanceFactory;
            _visualStudioWorkspace = visualStudioWorkspace;
            _serviceProvider = serviceProvider;
            _projectCodeModelFactory = projectCodeModelFactory;
        }

        public void OnProjectClosed()
        {
            _codeModelCache?.OnProjectClosed();
            _projectCodeModelFactory.OnProjectClosed(_projectId);
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

                    if (workspaceProject != null)
                    {
                        _codeModelCache = new CodeModelProjectCache(_threadingContext, _projectId, _codeModelInstanceFactory, _projectCodeModelFactory, _serviceProvider, workspaceProject.LanguageServices, _visualStudioWorkspace);
                    }
                }

                return _codeModelCache;
            }
        }

        internal IEnumerable<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>> GetCachedFileCodeModelInstances()
        {
            return GetCodeModelCache().GetFileCodeModelInstances();
        }

        internal bool TryGetCachedFileCodeModel(string fileName, out ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> fileCodeModelHandle)
        {
            var handle = GetCodeModelCache()?.GetComHandleForFileCodeModel(fileName);

            fileCodeModelHandle = handle != null
                ? handle.Value
                : default;

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
            // This uses the field directly. If we haven't yet created the CodeModelProjectCache, then we most definitely
            // don't have any source files we need to zombie when they go away. There's no reason to create a cache in that case.
            _codeModelCache?.OnSourceFileRemoved(fileName);
        }

        public void OnSourceFileRenaming(string filePath, string newFilePath)
        {
            // This uses the field directly. If we haven't yet created the CodeModelProjectCache, then we most definitely
            // don't have any source files we need to handle a rename for. There's no reason to create a cache in that case.
            _codeModelCache?.OnSourceFileRenaming(filePath, newFilePath);
        }

        EnvDTE.FileCodeModel IProjectCodeModel.GetOrCreateFileCodeModel(string filePath, object parent)
        {
            return this.GetOrCreateFileCodeModel(filePath, parent).Handle;
        }
    }
}
