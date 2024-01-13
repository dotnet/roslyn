// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// MEF metadata class used to find exports declared for a specific language.
    /// </summary>
    internal class LanguageMetadata : ILanguageMetadata
    {
        public string Language { get; }

        public LanguageMetadata(IDictionary<string, object> data)
            => this.Language = (string)data.GetValueOrDefault("Language");

        public LanguageMetadata(string language)
            => this.Language = language;
    }
}
