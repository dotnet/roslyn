// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Use this attribute to declare a <see cref="CodeRefactoringProvider"/> implementation so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExportCodeRefactoringProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// The name of the <see cref="CodeRefactoringProvider"/>.  
        /// </summary>
        [DisallowNull]
        public string? Name { get; set; }

        /// <summary>
        /// The source languages for which this provider can provide refactorings. See <see cref="LanguageNames"/>.
        /// </summary>
        public string[] Languages { get; }

        /// <summary>
        /// Attribute constructor used to specify availability of a code refactoring provider.
        /// </summary>
        /// <param name="firstLanguage">One language to which the code refactoring provider applies.</param>
        /// <param name="additionalLanguages">Additional languages to which the code refactoring provider applies. See <see cref="LanguageNames"/>.</param>
        public ExportCodeRefactoringProviderAttribute(string firstLanguage, params string[] additionalLanguages)
            : base(typeof(CodeRefactoringProvider))
        {
            if (additionalLanguages == null)
            {
                throw new ArgumentNullException(nameof(additionalLanguages));
            }

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
