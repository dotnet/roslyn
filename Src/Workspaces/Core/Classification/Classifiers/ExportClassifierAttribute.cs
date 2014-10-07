using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportClassifierAttribute : ExportAttribute
    {
        public string Name { get; private set; }
        public string Language { get; private set; }

        public ExportClassifierAttribute(
            string name,
            string language)
            : base(typeof(ISyntaxClassifier))
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (language == null)
            {
                throw new ArgumentNullException("language");
            }

            this.Name = name;
            this.Language = language;
        }
    }
}