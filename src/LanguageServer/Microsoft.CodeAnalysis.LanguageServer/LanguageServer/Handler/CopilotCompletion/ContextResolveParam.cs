// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Copilot;

internal sealed class ContextResolveParam
{
    [JsonPropertyName("documentContext")]
    public required TextDocumentPositionParams DocumentContext { get; set; }

    [JsonPropertyName("completionId")]
    public required string CompletionId { get; set; }

    [JsonPropertyName("timeBudget")]
    public required int TimeBudget { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("activeExperiments")]
    public Dictionary<string, ActiveExperiment>? ActiveExperiments { get; set; }

    public IReadOnlyDictionary<string, object> GetUnpackedActiveExperiments()
    {
        if (this.ActiveExperiments is null)
        {
            return ImmutableDictionary<string, object>.Empty;
        }

        var result = new Dictionary<string, object>(this.ActiveExperiments.Count);
        foreach (var kvp in this.ActiveExperiments)
        {
            result[kvp.Key] = kvp.Value.Value;
        }
        return result;
    }

    [Union, JsonConverter(typeof(UnionConverter))]
    internal sealed class ActiveExperiment : IUnion
    {
        public ActiveExperiment(string value) => Value = value;
        public ActiveExperiment(int value) => Value = value;
        public ActiveExperiment(bool value) => Value = value;
        public ActiveExperiment(string[] value) => Value = value;
        public object Value { get; }
    }
}
