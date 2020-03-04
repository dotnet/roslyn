// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class DiagnosticProviderMetadata : ILanguageMetadata
    {
        public string Name { get; }
        public string Language { get; }

        public DiagnosticProviderMetadata(IDictionary<string, object> data)
        {
            Name = (string)data.GetValueOrDefault("Name");
            Language = (string)data.GetValueOrDefault("Language");
        }
    }
}
