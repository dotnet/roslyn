// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef;

internal class CodeChangeProviderMetadata : OrderableMetadata, ILanguagesMetadata
{
    public IEnumerable<string> Languages { get; }

    public CodeChangeProviderMetadata(IDictionary<string, object> data)
        : base(data)
    {
        this.Languages = ((IReadOnlyDictionary<string, object>)data).GetEnumerableMetadata<string>("Languages");
    }

    public CodeChangeProviderMetadata(string name, IEnumerable<string> after = null, IEnumerable<string> before = null, params string[] languages)
        : base(name, after, before)
    {
        this.Languages = languages;
    }
}
