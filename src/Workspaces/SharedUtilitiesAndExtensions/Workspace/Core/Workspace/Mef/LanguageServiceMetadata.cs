// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// MEF metadata class used for finding <see cref="ILanguageService"/> and <see cref="ILanguageServiceFactory"/> exports.
    /// </summary>
    internal class LanguageServiceMetadata(IDictionary<string, object> data) : LanguageMetadata(data)
    {
        public string ServiceType { get; } = (string)data.GetValueOrDefault("ServiceType");
        public string Layer { get; } = (string)data.GetValueOrDefault("Layer");

        public IReadOnlyDictionary<string, object> Data { get; } = (IReadOnlyDictionary<string, object>)data;
    }
}
