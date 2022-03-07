// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// Use this attribute to export a <see cref="IEmbeddedLanguageClassifier"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportEmbeddedLanguageClassifierAttribute : ExportAttribute
    {
        /// <summary>
        /// Name of the classifier.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Name of the containing language hosting the embedded language.  e.g. C# or VB.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// Identifier in code (or StringSyntaxAttribute) used to identify an embedded language string. For example
        /// <c>Regex</c> or <c>Json</c>.
        /// </summary>
        /// <remarks>This can be used to find usages of an embedded language using a comment marker like <c>//
        /// lang=regex</c> or passed to a symbol annotated with <c>[StringSyntaxAttribyte("Regex")]</c>.  The identifier
        /// is case sensitive for the StringSyntaxAttribute, and case insensitive for the comment.
        /// </remarks>
        public string? Identifier { get; }

        public ExportEmbeddedLanguageClassifierAttribute(string name, string language, string? identifier)
            : base(typeof(IEmbeddedLanguageClassifier))
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Language = language ?? throw new ArgumentNullException(nameof(language));
            Identifier = identifier;
        }
    }
}
