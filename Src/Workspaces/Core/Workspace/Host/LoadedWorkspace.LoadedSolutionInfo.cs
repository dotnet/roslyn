using System;
using System.Collections.Generic;
using System.Linq;

namespace Roslyn.Services.Host
{
    internal partial class LoadedWorkspace
    {
        internal class LoadedSolutionInfo : ISolutionInfo
        {
            public SolutionId Id { get; private set; }
            public VersionStamp Version { get; private set; }
            public string FilePath { get; private set; }
            public IDictionary<string, string> GlobalProperties { get; private set; }

            internal readonly ILanguageServiceProviderFactory LanguageServiceProviderFactory;
            internal readonly ITextFactoryService TextFactory;
            internal readonly IProjectFileCache ProjectCache;
            
            private readonly object gate = new object();
            private readonly List<LoadedProjectInfo> projects = new List<LoadedProjectInfo>();
            private readonly Dictionary<Guid, ProjectId> projectIdMap = new Dictionary<Guid, ProjectId>();

            public LoadedSolutionInfo(
                SolutionId id,
                VersionStamp version,
                string filePath,
                ILanguageServiceProviderFactory languageServiceProviderFactory,
                ITextFactoryService textFactory,
                int maxProjectCacheSize = 10,
                IDictionary<string, string> globalProperties = null)
            {
                this.Id = id;
                this.Version = version;
                this.FilePath = filePath;
                this.LanguageServiceProviderFactory = languageServiceProviderFactory;
                this.TextFactory = textFactory;
                this.GlobalProperties = globalProperties;
                this.ProjectCache = new ProjectFileCache(maxProjectCacheSize, globalProperties);
            }

            internal void Clear()
            {
                lock (this.gate)
                {
                    projects.Clear();
                    projectIdMap.Clear();
                }
            }

            public LoadedProjectInfo AddProject(Guid projectGuid, string absoluteProjectPath, string language, bool standAloneProject)
            {
                var hostProject = new LoadedProjectInfo(
                    this,
                    projectGuid,
                    absoluteProjectPath,
                    this.TextFactory,
                    language,
                    standAloneProject);

                this.AddProject(hostProject);
                return hostProject;
            }

            internal LoadedProjectInfo CreateHostProject(ProjectId projectId, Guid projectGuid, string absoluteProjectPath, string language, bool standAloneProject)
            {
                return new LoadedProjectInfo(
                    projectId,
                    this,
                    projectGuid,
                    absoluteProjectPath,
                    this.TextFactory,
                    language,
                    standAloneProject);
            }

            public void AddProject(LoadedProjectInfo project)
            {
                lock (this.gate)
                {
                    projects.Add(project);
                    projectIdMap.Add(project.Guid, project.Id);
                }
            }

            public void RemoveProject(LoadedProjectInfo project)
            {
                lock (this.gate)
                {
                    projects.Remove(project);
                }
            }

            public IEnumerable<LoadedProjectInfo> Projects
            {
                get
                {
                    lock (this.gate)
                    {
                        return projects.ToList();
                    }
                }
            }

            IEnumerable<IProjectInfo> ISolutionInfo.Projects
            {
                get { return this.Projects; }
            }

            internal ProjectId GetId(Guid guid)
            {
                lock (this.gate)
                {
                    ProjectId id;
                    this.projectIdMap.TryGetValue(guid, out id);
                    return id;
                }
            }
        }
    }
}
