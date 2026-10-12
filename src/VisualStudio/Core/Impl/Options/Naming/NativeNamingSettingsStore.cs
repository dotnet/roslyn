// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.RpcContracts.UnifiedSettings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

/// <summary>
/// Converts the native broker's JSON values to the complete Naming graph. This adapter is not an editor
/// validator: native UI commits do not pass through it, so it must not be used to justify editable registration.
/// </summary>
internal sealed class NativeNamingSettingsStore
{
    private readonly IUnifiedSettingsService _settings;
    private readonly string[] _monikers;
    private readonly ChangeSource _source = new(nameof(NativeNamingSettingsStore), Guid.NewGuid());

    public NativeNamingSettingsStore(IUnifiedSettingsService settings, string language)
    {
        _settings = settings;
        var prefix = language switch
        {
            LanguageNames.CSharp => "languages.csharp.codeStyle.naming.",
            LanguageNames.VisualBasic => "languages.basic.codeStyle.naming.",
            _ => throw ExceptionUtilities.UnexpectedValue(language),
        };
        _monikers = [prefix + "symbolSpecifications", prefix + "namingStyles", prefix + "rules"];
    }

    public async Task<NamingSettingsGraph> ReadAsync(CancellationToken cancellationToken)
    {
        // Effective defaults are required for reset. Missing/unregistered values are errors, not a default graph.
        var results = await _settings.GetValuesAsync(_monikers, ReadOptions.NoRequirements, cancellationToken).ConfigureAwait(false);
        var arrays = new ImmutableArray<IReadOnlyDictionary<string, object>>[_monikers.Length];
        for (var i = 0; i < _monikers.Length; i++)
        {
            var matches = results.Where(result => result.Moniker == _monikers[i]).ToArray();
            if (matches.Length != 1 || !matches[0].IsSuccess || matches[0].Value is not { ValueType: SettingValueType.ObjectArray } value)
                throw new InvalidDataException($"Unable to read naming setting '{_monikers[i]}'.");
            arrays[i] = ReadArray(value.Data);
        }

        var graph = new NamingSettingsGraph(NamingSettingsStorageCodec.DecodeSpecifications(arrays[0]), arrays[1], arrays[2]);
        _ = graph.ToPreferences();
        return graph;
    }

    public async Task<CommitResult> WriteAsync(NamingSettingsGraph graph, string description, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = graph.ToPreferences();
        var changes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [_monikers[0]] = JsonConvert.SerializeObject(NamingSettingsStorageCodec.EncodeSpecifications(graph.SymbolSpecifications)),
            [_monikers[1]] = JsonConvert.SerializeObject(graph.NamingStyles),
            [_monikers[2]] = JsonConvert.SerializeObject(graph.Rules),
        };
        var result = await _settings.RequestCommitAsync(_source, new SettingsCommit(changes, description), cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess && !(await ReadAsync(cancellationToken).ConfigureAwait(false)).ConfigurationEquals(graph))
            throw new InvalidDataException("The complete naming configuration was not published.");
        return result;
    }

    private static ImmutableArray<IReadOnlyDictionary<string, object>> ReadArray(string json)
    {
        try
        {
            using var textReader = new StringReader(json);
            using var reader = new JsonTextReader(textReader) { DateParseHandling = DateParseHandling.None };
            var token = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read() || token is not JArray array)
                throw new InvalidDataException("Naming settings must be arrays of objects.");

            var items = ImmutableArray.CreateBuilder<IReadOnlyDictionary<string, object>>();
            foreach (var item in array)
            {
                if (item is not JObject properties)
                    throw new InvalidDataException("Naming settings must be arrays of objects.");
                var values = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var property in properties.Properties())
                {
                    values.Add(property.Name, property.Value.Type switch
                    {
                        JTokenType.String => property.Value.Value<string>()!,
                        JTokenType.Boolean => property.Value.Value<bool>(),
                        JTokenType.Array when property.Value.All(element => element.Type == JTokenType.String)
                            => property.Value.Values<string>().ToArray(),
                        _ => throw new InvalidDataException($"Invalid naming property '{property.Name}'."),
                    });
                }
                items.Add(values);
            }
            return items.ToImmutable();
        }
        catch (JsonException e)
        {
            throw new InvalidDataException("Invalid naming settings JSON.", e);
        }
    }
}
