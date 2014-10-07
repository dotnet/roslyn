using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using MSB = Microsoft.Build;

namespace Roslyn.Services.Host
{
    [Export(typeof(IProjectFileService))]
    internal partial class ProjectFileService : IProjectFileService
    {
        private readonly Dictionary<Guid, string> guidToLanguageMap;
        private readonly Dictionary<string, string> extensionToLanguageMap = new Dictionary<string, string>();

        [ImportingConstructor]
        public ProjectFileService(
            [ImportMany] IEnumerable<Lazy<ILanguageService, IProjectFileLanguageMetadata>> projectLoaders)
        {
            this.guidToLanguageMap = projectLoaders.ToDictionary(lazy => Guid.Parse(lazy.Metadata.ProjectType), lazy => lazy.Metadata.Language);
            this.extensionToLanguageMap = projectLoaders.ToDictionary(lazy => lazy.Metadata.ProjectFileExtension, lazy => lazy.Metadata.Language, StringComparer.OrdinalIgnoreCase);
        }

        public IProjectFileCache CreateProjectFileCache(int maxProjects, IDictionary<string, string> globalProperties)
        {
            return new ProjectFileCache(maxProjects, globalProperties);
        }

        public string GetLanguageForProjectType(Guid projectType)
        {
            string language;
            this.guidToLanguageMap.TryGetValue(projectType, out language);
            return language;
        }

        public string GetLanguageForProjectFileExtension(string projectFileExtension)
        {
            string language;
            this.extensionToLanguageMap.TryGetValue(projectFileExtension, out language);
            return language;
        }

        private MSB.Evaluation.Project LoadProject(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            var collection = new MSB.Evaluation.ProjectCollection();
            return collection.LoadProject(path, globalProperties, toolsVersion: null);
        }
    }
}