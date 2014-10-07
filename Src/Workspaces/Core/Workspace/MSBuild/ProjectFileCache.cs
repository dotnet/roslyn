// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.ProjectFileLoader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class ProjectFileCache
    {
        private readonly IDictionary<string, string> globalProperties;
        private readonly Dictionary<string, IProjectFile> map;
        private readonly List<IProjectFile> mostRecentlyUsedList;
        private readonly int maxProjectCount;
        private readonly AsyncSemaphore gate = new AsyncSemaphore(1);

        public ProjectFileCache(int maxProjectCount, IDictionary<string, string> globalProperties = null)
        {
            this.map = new Dictionary<string, IProjectFile>();
            this.globalProperties = globalProperties;
            this.mostRecentlyUsedList = new List<IProjectFile>();
            this.maxProjectCount = maxProjectCount;
        }

        public async Task<IProjectFile> GetProjectFileAsync(string path, IProjectFileLoaderLanguageService loader, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (await this.gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                IProjectFile project;
                if (this.map.TryGetValue(path, out project))
                {
                    this.mostRecentlyUsedList.Remove(project);
                    this.mostRecentlyUsedList.Add(project); // bump to end...
                    return project;
                }
                else
                {
                    project = await loader.LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false);

                    this.mostRecentlyUsedList.Add(project);
                    this.map.Add(path, project);

                    // kick out any projects over the max project count
                    while (this.mostRecentlyUsedList.Count > this.maxProjectCount)
                    {
                        var unloadProject = this.mostRecentlyUsedList[0];
                        this.mostRecentlyUsedList.RemoveAt(0);
                        this.map.Remove(unloadProject.FilePath);
                    }

                    return project;
                }
            }
        }

        public IProjectFile GetProjectFile(string path, IProjectFileLoaderLanguageService loader, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (this.gate.DisposableWait(cancellationToken))
            {
                IProjectFile project;
                if (this.map.TryGetValue(path, out project))
                {
                    this.mostRecentlyUsedList.Remove(project);
                    this.mostRecentlyUsedList.Add(project); // bump to end...
                    return project;
                }
                else
                {
                    project = loader.LoadProject(path, globalProperties, cancellationToken);

                    this.mostRecentlyUsedList.Add(project);
                    this.map.Add(path, project);

                    // kick out any projects over the max project count
                    while (this.mostRecentlyUsedList.Count > this.maxProjectCount)
                    {
                        var unloadProject = this.mostRecentlyUsedList[0];
                        this.mostRecentlyUsedList.RemoveAt(0);
                        this.map.Remove(unloadProject.FilePath);
                    }

                    return project;
                }
            }
        }

        public void Remove(string path)
        {
            using (this.gate.DisposableWait())
            {
                IProjectFile projectFile;
                if (this.map.TryGetValue(path, out projectFile))
                {
                    this.map.Remove(path);
                    this.mostRecentlyUsedList.Remove(projectFile);
                }
            }
        }

        public void Clear()
        {
            using (this.gate.DisposableWait())
            {
                this.map.Clear();
                this.mostRecentlyUsedList.Clear();
            }
        }
    }
}