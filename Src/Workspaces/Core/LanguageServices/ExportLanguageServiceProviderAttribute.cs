using System;
using System.ComponentModel.Composition;

namespace Roslyn.Services
{
    /// <summary>
    /// Specifies the exact type of the service exported by the ILanguageService.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportLanguageServiceProviderAttribute : ExportAttribute
    {
        public string Language { get; private set; }

        public ExportLanguageServiceProviderAttribute(string language)
            : base(typeof(ILanguageServiceProvider))
        {
            if (language == null)
            {
                throw new ArgumentNullException("language");
            }

            this.Language = language;
        }
    }
}