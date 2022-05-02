// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IEmbeddedLanguageClassifier
    {
        /// <summary>
        /// Identifiers in code (or StringSyntaxAttribute) used to identify an embedded language string. For example
        /// <c>Regex</c> or <c>Json</c>.
        /// </summary>
        /// <remarks>This can be used to find usages of an embedded language using a comment marker like <c>//
        /// lang=regex</c> or passed to a symbol annotated with <c>[StringSyntaxAttribyte("Regex")]</c>.  The identifier
        /// is case sensitive for the StringSyntaxAttribute, and case insensitive for the comment.
        /// </remarks>
        ImmutableArray<string> Identifiers { get; }

        /// <summary>
        /// This method will be called for all string and character tokens in a file to determine if there are special
        /// embedded language strings to classify.
        /// </summary>
        void RegisterClassifications(EmbeddedLanguageClassificationContext context);
    }
}
