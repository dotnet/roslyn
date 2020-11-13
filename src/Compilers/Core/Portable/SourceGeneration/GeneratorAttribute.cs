// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Place this attribute onto a type to cause it to be considered a source generator
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratorAttribute : Attribute
    {
        /// <summary>
        /// The source languages to which this generator applies. See <see cref="LanguageNames"/>.
        /// </summary>
        public string[] Languages { get; }

        /// <summary>
        /// Attribute constructor used to specify the attached class is a source generator that provides CSharp sources.
        /// </summary>
        public GeneratorAttribute()
            : this(LanguageNames.CSharp) { }

        /// <summary>
        /// Attribute constructor used to specify the attached class is a source generator and indicate which language(s) it supports.
        /// </summary>
        /// <param name="firstLanguage">One language to which the generator applies.</param>
        /// <param name="additionalLanguages">Additional languages to which the generator applies. See <see cref="LanguageNames"/>.</param>
        public GeneratorAttribute(string firstLanguage, params string[] additionalLanguages)
        {
            if (firstLanguage == null)
            {
                throw new ArgumentNullException(nameof(firstLanguage));
            }

            if (additionalLanguages == null)
            {
                throw new ArgumentNullException(nameof(additionalLanguages));
            }

            var languages = new string[additionalLanguages.Length + 1];
            languages[0] = firstLanguage;
            for (int index = 0; index < additionalLanguages.Length; index++)
            {
                languages[index + 1] = additionalLanguages[index];
            }

            this.Languages = languages;
        }

    }
}
