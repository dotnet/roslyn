// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Cache FileCodeModel instances for a given project (we are using WeakReference for now, 
    /// so that we can more or less match the semantics of the former native implementation, which 
    /// offered reference equality until all instances were collected by the GC)
    /// </summary>
    internal sealed partial class CodeModelProjectCache
    {
        private readonly ProjectId _projectId;
        private readonly ICodeModelInstanceFactory _codeModelInstanceFactory;

        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly object _cacheGate = new object();

        private EnvDTE.CodeModel? _rootCodeModel;
        private bool _zombied;

        internal CodeModelState State { get; }

        internal CodeModelProjectCache(
            IThreadingContext threadingContext,
            ProjectId projectId,
            ICodeModelInstanceFactory codeModelInstanceFactory,
            ProjectCodeModelFactory projectFactory,
            IServiceProvider serviceProvider,
            Microsoft.CodeAnalysis.Host.LanguageServices languageServices,
            VisualStudioWorkspace workspace)
        {
            State = new CodeModelState(threadingContext, serviceProvider, languageServices, workspace, projectFactory);
            _projectId = projectId;
            _codeModelInstanceFactory = codeModelInstanceFactory;
        }

        /// <summary>
        /// Look for an existing instance of FileCodeModel in our cache.
        /// Return null if there is no active FCM for "fileName".
        /// </summary>
        private CacheEntry? GetCacheEntry(string fileName)
        {
            lock (_cacheGate)
            {
                if (_cache.TryGetValue(fileName, out var cacheEntry))
                {
                    return cacheEntry;
                }
            }

            return null;
        }

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> GetOrCreateFileCodeModel(string filePath)
        {
            // First try
            {
                var cacheEntry = GetCacheEntry(filePath);
                if (cacheEntry != null)
                {
                    var comHandle = cacheEntry.Value.ComHandle;
                    if (comHandle != null)
                    {
                        return comHandle.Value;
                    }
                }
            }

            // This ultimately ends up calling GetOrCreateFileCodeModel(fileName, parent) with the correct "parent" object
            // through the project system.
            var newFileCodeModel = (EnvDTE80.FileCodeModel2)_codeModelInstanceFactory.TryCreateFileCodeModelThroughProjectSystem(filePath);
            return new ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>(newFileCodeModel);
        }

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? GetComHandleForFileCodeModel(string filePath)
        {
            var cacheEntry = GetCacheEntry(filePath);

            return cacheEntry?.ComHandle;
        }

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> GetOrCreateFileCodeModel(string filePath, object? parent)
        {
            // First try
            {
                var cacheEntry = GetCacheEntry(filePath);
                if (cacheEntry != null)
                {
                    var comHandle = cacheEntry.Value.ComHandle;
                    if (comHandle != null)
                    {
                        return comHandle.Value;
                    }
                }
            }

            // Check that we know about this file!
            var solution = State.Workspace.CurrentSolution;
            var documentId = solution.GetDocumentIdsWithFilePath(filePath).Where(id => id.ProjectId == _projectId).FirstOrDefault();
            if (documentId == null || solution.GetDocument(documentId) == null)
            {
                // Matches behavior of native (C#) implementation
                throw Exceptions.ThrowENotImpl();
            }

            // Create object (outside of lock)
            var newFileCodeModel = FileCodeModel.Create(State, parent, documentId, isSourceGeneratorOutput: false, new TextManagerAdapter());
            var newCacheEntry = new CacheEntry(newFileCodeModel);

            // Second try (object might have been added by another thread at this point!)
            lock (_cacheGate)
            {
                var cacheEntry = GetCacheEntry(filePath);
                if (cacheEntry != null)
                {
                    var comHandle = cacheEntry.Value.ComHandle;
                    if (comHandle != null)
                    {
                        return comHandle.Value;
                    }
                }

                // Note: Using the indexer here (instead of "Add") is relevant since the old
                //       WeakReference entry is likely still in the cache (with a Null target, of course)
                _cache[filePath] = newCacheEntry;

                return newFileCodeModel;
            }
        }

        public EnvDTE.CodeModel GetOrCreateRootCodeModel(EnvDTE.Project parent)
        {
            if (_zombied)
            {
                Debug.Fail("Cannot access root code model after code model was shutdown!");
                throw Exceptions.ThrowEUnexpected();
            }

            _rootCodeModel ??= RootCodeModel.Create(State, parent, _projectId);

            return _rootCodeModel;
        }

        public IEnumerable<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>> GetFileCodeModelInstances()
        {
            var result = new List<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>>();

            lock (_cacheGate)
            {
                foreach (var cacheEntry in _cache.Values)
                {
                    var comHandle = cacheEntry.ComHandle;
                    if (comHandle != null)
                    {
                        result.Add(comHandle.Value);
                    }
                }
            }

            return result;
        }

        public void OnProjectClosed()
        {
            var instances = GetFileCodeModelInstances();

            lock (_cacheGate)
            {
                _cache.Clear();
            }

            foreach (var instance in instances)
            {
                instance.Object.Shutdown();
            }

            _zombied = true;
        }

        public void OnSourceFileRemoved(string fileName)
        {
            ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? comHandle = null;

            lock (_cacheGate)
            {
                if (_cache.TryGetValue(fileName, out var cacheEntry))
                {
                    comHandle = cacheEntry.ComHandle;
                    _cache.Remove(fileName);
                }
            }

            comHandle?.Object.Shutdown();
        }

        public void OnSourceFileRenaming(string oldFileName, string newFileName)
        {
            ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? comHandleToRename = null;
            ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? comHandleToShutDown = null;

            lock (_cacheGate)
            {
                if (_cache.TryGetValue(oldFileName, out var cacheEntry))
                {
                    comHandleToRename = cacheEntry.ComHandle;

                    _cache.Remove(oldFileName);

                    if (comHandleToRename != null)
                    {
                        // We might already have a code model for this new filename. This can happen if
                        // we were to rename Goo.cs to Goocs, which will call this method, and then rename
                        // it back, which does not call this method. This results in both Goo.cs and Goocs
                        // being in the cache. We could fix that "correctly", but the zombied Goocs code model
                        // is pretty broken, so there's no point in trying to reuse it.
                        if (_cache.TryGetValue(newFileName, out cacheEntry))
                        {
                            comHandleToShutDown = cacheEntry.ComHandle;
                        }

                        _cache.Add(newFileName, cacheEntry);
                    }
                }
            }

            comHandleToShutDown?.Object.Shutdown();
            comHandleToRename?.Object.OnRename(newFileName);
        }
    }
}
