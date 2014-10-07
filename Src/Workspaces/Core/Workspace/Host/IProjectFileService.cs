using System;
using System.Collections.Generic;

namespace Roslyn.Services.Host
{
    public interface IProjectFileService
    {
        IProjectFileCache CreateProjectFileCache(int maxProjects, IDictionary<string, string> globalProperties = null);
        string GetLanguageForProjectType(Guid projectType);
        string GetLanguageForProjectFileExtension(string projectFileExtension);
    }
}