// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
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
        private readonly CodeModelState _state;
        private readonly AbstractProject _project;

        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly object _cacheGate = new object();

        private EnvDTE.CodeModel _rootCodeModel;
        private bool _zombied;

        internal CodeModelProjectCache(AbstractProject project, IServiceProvider serviceProvider, HostLanguageServices languageServices, VisualStudioWorkspace workspace)
        {
            _project = project;
            _state = new CodeModelState(serviceProvider, languageServices, workspace);
        }

        private bool IsZombied
        {
            get { return _zombied; }
        }

        /// <summary>
        /// Look for an existing instance of FileCodeModel in our cache.
        /// Return null if there is no active FCM for "fileName".
        /// </summary>
        private CacheEntry? GetCacheEntry(string fileName)
        {
            lock (_cacheGate)
            {
                CacheEntry cacheEntry;
                if (_cache.TryGetValue(fileName, out cacheEntry))
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
            var provider = (IProjectCodeModelProvider)_project;
            var newFileCodeModel = (EnvDTE80.FileCodeModel2)provider.ProjectCodeModel.CreateFileCodeModelThroughProject(filePath);
            return new ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>(newFileCodeModel);
        }

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? GetComHandleForFileCodeModel(string filePath)
        {
            var cacheEntry = GetCacheEntry(filePath);

            return cacheEntry != null
                ? cacheEntry.Value.ComHandle
                : null;
        }

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> GetOrCreateFileCodeModel(string filePath, object parent)
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
            var hostDocument = _project.GetCurrentDocumentFromPath(filePath);
            if (hostDocument == null)
            {
                // Matches behavior of native (C#) implementation
                throw Exceptions.ThrowENotImpl();
            }

            // Create object (outside of lock)
            var newFileCodeModel = FileCodeModel.Create(_state, parent, hostDocument.Id, new TextManagerAdapter());
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
            if (this.IsZombied)
            {
                Debug.Fail("Cannot access root code model after code model was shutdown!");
                throw Exceptions.ThrowEUnexpected();
            }

            if (_rootCodeModel == null)
            {
                _rootCodeModel = RootCodeModel.Create(_state, parent, _project.Id);
            }

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

            _zombied = false;
        }

        public void OnSourceFileRemoved(string fileName)
        {
            ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? comHandle = null;

            lock (_cacheGate)
            {
                CacheEntry cacheEntry;
                if (_cache.TryGetValue(fileName, out cacheEntry))
                {
                    comHandle = cacheEntry.ComHandle;
                    _cache.Remove(fileName);
                }
            }

            if (comHandle != null)
            {
                comHandle.Value.Object.Shutdown();
            }
        }

        public void OnSourceFileRenaming(string oldFileName, string newFileName)
        {
            ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? comHandle = null;

            lock (_cacheGate)
            {
                CacheEntry cacheEntry;
                if (_cache.TryGetValue(oldFileName, out cacheEntry))
                {
                    comHandle = cacheEntry.ComHandle;

                    _cache.Remove(oldFileName);

                    if (comHandle != null)
                    {
                        _cache.Add(newFileName, cacheEntry);
                    }
                }
            }

            if (comHandle != null)
            {
                comHandle.Value.Object.OnRename(newFileName);
            }
        }
    }
}
