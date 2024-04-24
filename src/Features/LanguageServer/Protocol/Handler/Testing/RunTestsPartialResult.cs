// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

internal record RunTestsPartialResult(
    [property: JsonPropertyName("stage")] string Stage,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("progress"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)] TestProgress? Progress
);
