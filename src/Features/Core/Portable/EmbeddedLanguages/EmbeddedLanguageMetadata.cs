// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages;

internal sealed class EmbeddedLanguageMetadata : OrderableMetadata, ILanguagesMetadata
{
    /// <summary>
    /// The particular language-IDs this language supports (for example 'regex/regexp/etc.').
    /// </summary>
    public IEnumerable<string> Identifiers { get; }

    public IEnumerable<string> Languages { get; }

    /// <summary>
    /// If this language supports strings being passed to APIs that do not have a <c>// lang=...</c> comment or a
    /// <c>[StringSyntax]</c> attribute on them.  This is not exposed publicly as all modern language plugins should
    /// use those mechanisms.  This is for Regex/Json to support lighting up on older platform APIs that shipped
    /// before this IDE capability and thus were never annotated.
    /// </summary>
    internal bool SupportsUnannotatedAPIs { get; }

    public EmbeddedLanguageMetadata(IDictionary<string, object> data)
        : base(data)
    {
        this.Identifiers = ((IReadOnlyDictionary<string, object>)data).GetEnumerableMetadata<string>(nameof(Identifiers)).WhereNotNull();
        this.Languages = ((IReadOnlyDictionary<string, object>)data).GetEnumerableMetadata<string>(nameof(Languages)).WhereNotNull();
        this.SupportsUnannotatedAPIs = data.GetValueOrDefault(nameof(SupportsUnannotatedAPIs)) is bool b ? b : false;
    }

    public EmbeddedLanguageMetadata(
        string name, IEnumerable<string> languages, IEnumerable<string> after, IEnumerable<string> before, IEnumerable<string> identifiers, bool supportsUnannotatedAPIs)
        : base(name, after, before)
    {
        this.Identifiers = identifiers;
        this.Languages = languages;
        SupportsUnannotatedAPIs = supportsUnannotatedAPIs;
    }
}
