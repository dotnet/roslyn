// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Features.Intents;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class IntentProviderAttribute(string intentName, string languageName) : ExportAttribute(typeof(IIntentProvider)), IIntentProviderMetadata
{
    public string IntentName { get; } = intentName;
    public string LanguageName { get; } = languageName;
}
