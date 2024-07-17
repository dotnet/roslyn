// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host.Mef;

/// <summary>
/// MEF metadata class used to find exports declared for a specific language.
/// </summary>
internal class LanguageMetadata(IDictionary<string, object> data) : ILanguageMetadata
{
    public string Language { get; } = (string)data[nameof(Language)];
}
