// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    internal class EmbeddedLanguageMetadata : OrderableLanguageMetadata
    {
        public string? Identifier { get; }

        public EmbeddedLanguageMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.Identifier = (string)data.GetValueOrDefault("Identifier");
        }

        public EmbeddedLanguageMetadata(string name, string language, string? identifier, IEnumerable<string> after, IEnumerable<string> before)
            : base(name, language, after, before)
        {
            this.Identifier = identifier;
        }
    }
}
