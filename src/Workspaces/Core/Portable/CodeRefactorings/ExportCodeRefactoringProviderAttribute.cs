// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static readonly TextDocumentKind[] s_defaultDocumentKinds = new[] { TextDocumentKind.Document };

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
        /// The document kinds for which this provider can provide refactorings. See <see cref="TextDocumentKind"/>.
        /// By default, the provider supports refactorings only for source documents, <see cref="TextDocumentKind.Document"/>.
        /// </summary>
        public TextDocumentKind[] DocumentKinds { get; }

        /// <summary>
        /// The document extensions for which this provider can provide refactorings.
        /// By default, this value is null and the document extension is not considered to determine applicability of refactorings.
        /// </summary>
        public string[]? DocumentExtensions { get; }

        private ExportCodeRefactoringProviderAttribute(TextDocumentKind[] documentKinds, string[]? documentExtensions, string[] languages)
            : base(typeof(CodeRefactoringProvider))
        {
            this.DocumentKinds = documentKinds;
            this.DocumentExtensions = documentExtensions;
            this.Languages = languages;
        }

        private static string[] GetLanguages(string firstLanguage, string[] additionalLanguages)
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

            return languages;
        }

        /// <summary>
        /// Attribute constructor used to specify availability of a code refactoring provider in source docments for specific project language(s).
        /// </summary>
        /// <param name="firstLanguage">One language to which the code refactoring provider applies.</param>
        /// <param name="additionalLanguages">Additional languages to which the code refactoring provider applies. See <see cref="LanguageNames"/>.</param>
        public ExportCodeRefactoringProviderAttribute(string firstLanguage, params string[] additionalLanguages)
            : this(s_defaultDocumentKinds, documentExtensions: null, GetLanguages(firstLanguage, additionalLanguages))
        {
        }

        /// <summary>
        /// Attribute constructor used to specify availability of a code refactoring provider for specific document kinds and project language(s).
        /// </summary>
        /// <param name="documentKinds">Document kinds to which the code refactoring provider applies.</param>
        /// <param name="firstLanguage">One language to which the code refactoring provider applies.</param>
        /// <param name="additionalLanguages">Additional languages to which the code refactoring provider applies. See <see cref="LanguageNames"/>.</param>
        public ExportCodeRefactoringProviderAttribute(TextDocumentKind[] documentKinds, string firstLanguage, params string[] additionalLanguages)
            : this(documentKinds, documentExtensions: null, GetLanguages(firstLanguage, additionalLanguages))
        {
            if (documentKinds == null)
            {
                throw new ArgumentNullException(nameof(documentKinds));
            }
        }

        /// <summary>
        /// Attribute constructor used to specify availability of a code refactoring provider for specific document kinds, document extensions and project language(s).
        /// </summary>
        /// <param name="documentKinds">Document kinds to which the code refactoring provider applies.</param>
        /// <param name="documentExtensions">Document extensions to which the code refactoring provider applies.</param>
        /// <param name="firstLanguage">One language to which the code refactoring provider applies.</param>
        /// <param name="additionalLanguages">Additional languages to which the code refactoring provider applies. See <see cref="LanguageNames"/>.</param>
        public ExportCodeRefactoringProviderAttribute(TextDocumentKind[] documentKinds, string[] documentExtensions, string firstLanguage, params string[] additionalLanguages)
            : this(documentKinds, documentExtensions, GetLanguages(firstLanguage, additionalLanguages))
        {
            if (documentKinds == null)
            {
                throw new ArgumentNullException(nameof(documentKinds));
            }

            if (documentExtensions == null)
            {
                throw new ArgumentNullException(nameof(documentExtensions));
            }
        }
    }
}
