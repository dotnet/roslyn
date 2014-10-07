using System;

namespace Microsoft.CodeAnalysis
{
    public sealed class OpenProjectOptions
    {
        /// <summary>
        /// The language for the project. If not specified the language is inferred from the project file's extension.
        /// </summary>
        public string Language { get; private set; }

        /// <summary>
        /// If true the current solution is closed and a new one created before the project is opened.
        /// </summary>
        public bool NewSolution { get; private set; }

        /// <summary>
        /// Translates references to other projects into references to corresponding libraries.
        /// </summary>
        public bool TranslateProjectReferences { get; private set; }

        private OpenProjectOptions(
            string language,
            bool newSolution,
            bool translateProjectReferences)
        {
            this.Language = language;
            this.NewSolution = newSolution;
            this.TranslateProjectReferences = translateProjectReferences;
        }

        public OpenProjectOptions()
            : this(null, true, true)
        {
        }

        private OpenProjectOptions With(
            string language = null,
            bool? newSolution = null,
            bool? translateProjectReferences = null)
        {
            language = language ?? this.Language;
            newSolution = newSolution ?? this.NewSolution;
            translateProjectReferences = translateProjectReferences ?? this.TranslateProjectReferences;

            if (language != this.Language
                || newSolution.GetValueOrDefault() != this.NewSolution
                || translateProjectReferences.GetValueOrDefault() != this.TranslateProjectReferences)
            {
                return new OpenProjectOptions(language, newSolution.GetValueOrDefault(), translateProjectReferences.GetValueOrDefault());
            }

            return this;
        }

        public OpenProjectOptions WithLanguage(string language)
        {
            return this.With(language: language);
        }

        public OpenProjectOptions WithNewSolution(bool newSolution)
        {
            return this.With(newSolution: newSolution);
        }

        public OpenProjectOptions WithTranslateProjectReferences(bool translateProjectReferences)
        {
            return this.With(translateProjectReferences: translateProjectReferences);
        }

        public static readonly OpenProjectOptions StandaloneProject = new OpenProjectOptions();

        public static readonly OpenProjectOptions IncrementalProject = new OpenProjectOptions(language: null, newSolution: false, translateProjectReferences: false);
    }
}