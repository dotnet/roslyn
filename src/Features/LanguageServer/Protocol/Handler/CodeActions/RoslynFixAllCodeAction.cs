// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;

[DataContract]
internal sealed class RoslynFixAllCodeAction(string scope) : CodeAction
{
    [JsonProperty(PropertyName = "scope")]
    public string Scope { get; } = scope;
}
