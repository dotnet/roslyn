// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Use this attribute to declare a <see cref="CodeRefactoringProvider"/> implementation so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ExportCodeRefactoringProviderAttribute : ExportAttribute
    {
        private static readonly string[] s_defaultDocumentKinds = [nameof(TextDocumentKind.Document)];
        private static readonly string[] s_documentKindNames = Enum.GetNames(typeof(TextDocumentKind));

        private string[] _documentKinds;

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
        public string[] DocumentKinds
        {
            get => _documentKinds;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                foreach (var kind in value)
                {
                    if (kind == null || !s_documentKindNames.Contains(kind))
                    {
                        var message = string.Format(WorkspacesResources.Unexpected_value_0_in_DocumentKinds_array,
                            arg0: kind?.ToString() ?? "null");
                        throw new ArgumentException(message);
                    }
                }

                _documentKinds = value;
            }
        }

        /// <summary>
        /// The document extensions for which this provider can provide refactorings.
        /// Each extension string must include the leading period, for example, ".txt", ".xaml", ".editorconfig", etc.
        /// By default, this value is null and the document extension is not considered to determine applicability of refactorings.
        /// </summary>
        public string[]? DocumentExtensions { get; set; }

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
            this._documentKinds = s_defaultDocumentKinds;
            this.DocumentExtensions = null;
        }
    }
}
