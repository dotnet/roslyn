// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Use this attribute to declare a <see cref="CodeFixProvider"/> implementation so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ExportCodeFixProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// Optional name of the <see cref="CodeFixProvider"/>.  
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The source languages this provider can provide fixes for.  See <see cref="LanguageNames"/>.
        /// </summary>
        public string[] Languages { get; }

        /// <summary>
        /// Attribute constructor used to specify automatic application of a code fix provider.
        /// </summary>
        /// <param name="firstLanguage">One language to which the code fix provider applies.</param>
        /// <param name="additionalLanguages">Additional languages to which the code fix provider applies. See <see cref="LanguageNames"/>.</param>
        public ExportCodeFixProviderAttribute(
            string firstLanguage,
            params string[] additionalLanguages)
            : base(typeof(CodeFixProvider))
        {
            if (additionalLanguages == null)
            {
                throw new ArgumentNullException(nameof(additionalLanguages));
            }

            this.Name = null;

            var languages = new string[additionalLanguages.Length + 1];
            languages[0] = firstLanguage ?? throw new ArgumentNullException(nameof(firstLanguage));
            for (var index = 0; index < additionalLanguages.Length; index++)
            {
                languages[index + 1] = additionalLanguages[index];
            }

            this.Languages = languages;
        }
    }
}
