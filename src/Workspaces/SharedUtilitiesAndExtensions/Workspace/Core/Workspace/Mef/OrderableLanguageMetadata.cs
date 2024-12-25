// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef;

internal class OrderableLanguageMetadata : OrderableMetadata, ILanguageMetadata
{
    public string Language { get; }

    public OrderableLanguageMetadata(IDictionary<string, object> data)
        : base(data)
    {
        this.Language = (string)data.GetValueOrDefault("Language");
    }

    public OrderableLanguageMetadata(string name, string language, IEnumerable<string> after, IEnumerable<string> before)
        : base(name, after, before)
    {
        this.Language = language;
    }
}
