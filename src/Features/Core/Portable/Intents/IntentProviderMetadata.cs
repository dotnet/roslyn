// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Features.Intents;

/// <summary>
/// MEF metadata class used to find <see cref="IIntentProvider"/> exports.
/// </summary>
internal sealed class IntentProviderMetadata(IDictionary<string, object> data)
{
    public string IntentName { get; } = (string)data[nameof(IntentProviderAttribute.IntentName)];
    public string LanguageName { get; } = (string)data[nameof(IntentProviderAttribute.LanguageName)];
}
