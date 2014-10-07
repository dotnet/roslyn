using System;
using System.ComponentModel.Composition;

namespace Roslyn.Services.LanguageServices
{
    [MetadataAttribute]
    public class LanguageAttribute : Attribute
    {
        public string Language { get; private set; }

        public LanguageAttribute(string language)
        {
            this.Language = language;
        }
    }
}