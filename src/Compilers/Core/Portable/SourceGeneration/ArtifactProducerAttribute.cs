// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Place this attribute onto a type to cause it to be considered an artifact producer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ArtifactProducerAttribute : Attribute
    {
        /// <summary>
        /// The source languages to which this artifact producer applies. See <see cref="LanguageNames"/>.
        /// </summary>
        public string[] Languages { get; }

        /// <summary>
        /// Attribute constructor used to specify the attached class is a artifact producer and indicate which language(s) it supports.
        /// </summary>
        /// <param name="firstLanguage">One language to which the producer applies.</param>
        /// <param name="additionalLanguages">Additional languages to which the producer applies. See <see cref="LanguageNames"/>.</param>
        public ArtifactProducerAttribute(string firstLanguage, params string[] additionalLanguages)
        {
            if (firstLanguage == null)
                throw new ArgumentNullException(nameof(firstLanguage));

            if (additionalLanguages == null)
                throw new ArgumentNullException(nameof(additionalLanguages));

            var languages = new string[additionalLanguages.Length + 1];
            languages[0] = firstLanguage;
            Array.Copy(additionalLanguages, sourceIndex: 0, languages, destinationIndex: 1, additionalLanguages.Length);

            this.Languages = languages;
        }
    }
}
