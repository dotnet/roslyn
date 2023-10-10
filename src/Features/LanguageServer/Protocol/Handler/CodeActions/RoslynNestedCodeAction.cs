// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;

[DataContract]
internal sealed class RoslynNestedCodeAction(ImmutableArray<string> nestedActionsIdentifiers) : CodeAction
{
    [JsonProperty(PropertyName = "nestedActionsIdentifiers")]
    public ImmutableArray<string> NestedActionsIdentifiers { get; } = nestedActionsIdentifiers;
}
