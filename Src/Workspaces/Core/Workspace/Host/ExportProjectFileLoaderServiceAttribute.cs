using System;
using System.ComponentModel.Composition;

namespace Roslyn.Services.Host
{
    /// <summary>
    /// Specifies the exact type of the service exported by the ILanguageService
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportProjectFileLoaderServiceAttribute : ExportLanguageServiceAttribute
    {
        public ExportProjectFileLoaderServiceAttribute(Type type, string language, string projectType, string projectFileExtension)
            : base(type, language)
        {
            this.ProjectType = projectType;
            this.ProjectFileExtension = projectFileExtension;
        }

        public string ProjectType { get; private set; }
        public string ProjectFileExtension { get; private set; }
    }
}