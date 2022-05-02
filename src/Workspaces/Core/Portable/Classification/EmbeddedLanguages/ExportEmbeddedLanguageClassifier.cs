// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// Use this attribute to export a <see cref="IEmbeddedLanguageClassifier"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportEmbeddedLanguageClassifierAttribute : ExportAttribute
    {
        /// <summary>
        /// Name of the classifier.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Name of the containing language hosting the embedded language.  e.g. C# or VB.
        /// </summary>
        public string Language { get; }

        public ExportEmbeddedLanguageClassifierAttribute(
            string name, string language)
            : base(typeof(IEmbeddedLanguageClassifier))
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }

    /// <summary>
    /// Internal version of ExportEmbeddedLanguageClassifierAttribute.  Used so we can allow regex/json to still light
    /// up on legacy APIs not using the new [StringSyntax] attribute the runtime added.  For public extensions that's
    /// the only mechanism we support.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportEmbeddedLanguageClassifierInternalAttribute : ExportEmbeddedLanguageClassifierAttribute
    {
        /// <inheritdoc cref="EmbeddedLanguageMetadata.SupportsUnannotatedAPIs"/>
        public bool SupportsUnannotatedAPIs { get; }

        public ExportEmbeddedLanguageClassifierInternalAttribute(
            string name, string language, bool supportsUnannotatedAPIs)
            : base(name, language)
        {
            SupportsUnannotatedAPIs = supportsUnannotatedAPIs;
        }
    }
}
