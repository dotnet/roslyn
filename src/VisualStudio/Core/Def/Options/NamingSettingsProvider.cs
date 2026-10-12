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
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class NamingSettingsProvider : ICachingExternalSettingsProvider, IExternalSettingValidator, IDisposable
{
    private readonly NamingSettingsCoordinator _coordinator;
    private readonly string _language;
    private readonly object _gate = new();
    private NamingSettingsGraph? _snapshot;
    private NamingSettingsGraph? _pending;

    public NamingSettingsProvider(NamingSettingsCoordinator coordinator, string language = LanguageNames.CSharp)
    {
        if (language is not (LanguageNames.CSharp or LanguageNames.VisualBasic))
            throw new ArgumentException("Unsupported Naming settings language.", nameof(language));
        _coordinator = coordinator;
        _language = language;
        _coordinator.Changed += OnChanged;
    }

    public event EventHandler<ExternalSettingsChangedEventArgs>? SettingValuesChanged;
    public event EventHandler<EnumSettingChoicesChangedEventArgs>? EnumSettingChoicesChanged;
    public event EventHandler? RevalidationRequired;
    public event EventHandler? ErrorConditionResolved;
    public event EventHandler<DynamicMessageTextChangedEventArgs> DynamicMessageTextChanged { add { } remove { } }

    public async Task<ExternalSettingOperationResult<T>> GetValueAsync<T>(string moniker, CancellationToken cancellationToken) where T : notnull
    {
        CheckMoniker(moniker);
        try
        {
            var graph = await GetGraphAsync(cancellationToken).ConfigureAwait(false);
            var preferences = graph.ToPreferences();
            var items = moniker switch
            {
                "rules" => graph.Rules.Select(CloneItem).ToArray(),
                "symbolSpecifications" => graph.SymbolSpecifications.Select((item, index) => DisplayDefinition(item, preferences.SymbolSpecifications[index].Name ?? "", isStyle: false)).ToArray(),
                "namingStyles" => graph.NamingStyles.Select((item, index) => DisplayDefinition(item, preferences.NamingStyles[index].Name ?? "", isStyle: true)).ToArray(),
                _ => throw new InvalidDataException($"Unknown naming setting '{moniker}'."),
            };
            if (items is T value)
                return new ExternalSettingOperationResult<T>.Success(value);

            throw new InvalidDataException("Naming settings must be requested as an array of objects.");
        }
        catch (InvalidDataException e)
        {
            return new ExternalSettingOperationResult<T>.Failure(GetError(e.Message), ExternalSettingsErrorScope.EntireRegion, isTransient: true);
        }
    }

    public async Task<ExternalSettingOperationResult> SetValueAsync<T>(string moniker, T value, CancellationToken cancellationToken) where T : notnull
    {
        CheckMoniker(moniker);
        try
        {
            var graph = await GetGraphAsync(cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                var pending = UpdateGraph(_pending ?? _snapshot ?? graph, moniker, value);
                _ = pending.ToPreferences();
                _pending = pending;
            }

            RaiseRefresh();
            return ExternalSettingOperationResult.Success.Instance;
        }
        catch (InvalidDataException e)
        {
            return Failure(e.Message);
        }
    }

    public async Task<ExternalSettingOperationResult> RefreshCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            var graph = await _coordinator.ReadAsync(cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                _snapshot = graph;
                _pending = null;
            }

            RaiseRefresh();
            ErrorConditionResolved?.Invoke(this, EventArgs.Empty);
            return ExternalSettingOperationResult.Success.Instance;
        }
        catch (InvalidDataException e)
        {
            return Failure(e.Message);
        }
    }

    public async Task<ExternalSettingOperationResult> CommitPendingChangesAsync(CancellationToken cancellationToken)
    {
        NamingSettingsGraph? pending;
        NamingSettingsGraph? snapshot;
        lock (_gate)
        {
            pending = _pending;
            snapshot = _snapshot;
        }

        if (pending is null)
            return ExternalSettingOperationResult.Success.Instance;

        if (snapshot is null)
            return Failure("Naming settings must be refreshed before committing changes.");

        return await ApplyGraphAsync(pending, snapshot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NamingSettingsGraph> GetGraphAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if ((_pending ?? _snapshot) is { } graph)
                return graph;
        }

        var current = await _coordinator.ReadAsync(cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            return _pending ?? (_snapshot ??= current);
        }
    }

    public async Task<ExternalSettingOperationResult> ApplyGraphAsync(NamingSettingsGraph graph, NamingSettingsGraph expected, CancellationToken cancellationToken)
    {
        NamingSettingsGraph? pending;
        lock (_gate)
            pending = _pending;
        try
        {
            graph = graph.NormalizeDefaultNames();
            var result = await _coordinator.ApplyAsync(graph, expected, ServicesVSResources.Naming_rules, cancellationToken).ConfigureAwait(false);
            if (result.Outcome is not (SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued))
                return Failure($"{result.Outcome}. {result.Message}");

            lock (_gate)
            {
                _snapshot = graph;
                if (ReferenceEquals(_pending, pending))
                    _pending = null;
            }
            RaiseRefresh();
            NotifyErrorConditionResolved();
            return ExternalSettingOperationResult.Success.Instance;
        }
        catch (InvalidDataException e)
        {
            return Failure(e.Message);
        }
    }

    public async Task<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>> GetEnumChoicesAsync(string enumSettingMoniker, CancellationToken cancellationToken)
    {
        try
        {
            var graph = await GetGraphAsync(cancellationToken).ConfigureAwait(false);
            var preferences = graph.ToPreferences();
            IReadOnlyList<EnumChoice> choices = enumSettingMoniker switch
            {
                "rules.symbolSpecificationId" => graph.SymbolSpecifications.Select((item, index) => new EnumChoice(
                    NamingSettingsGraph.ReadString(item, "id"), preferences.SymbolSpecifications[index].Name ?? "")).ToArray(),
                "rules.namingStyleId" => graph.NamingStyles.Select((item, index) => new EnumChoice(
                    NamingSettingsGraph.ReadString(item, "id"), preferences.NamingStyles[index].Name ?? "")).ToArray(),
                "rules.severity" => [
                    new("silent", WorkspacesResources.Refactoring_Only),
                    new("suggestion", WorkspacesResources.Suggestion),
                    new("warning", WorkspacesResources.Warning),
                    new("error", WorkspacesResources.Error),
                ],
                "symbolSpecifications.symbolKinds" => GetSymbolKindChoices(graph),
                "symbolSpecifications.accessibilities" => _language == LanguageNames.CSharp
                    ? [new("public", "public"), new("internal", "internal"), new("private", "private"), new("protected", "protected"), new("protectedInternal", "protected internal"), new("privateProtected", "private protected"), new("local", "local")]
                    : [new("public", "Public"), new("internal", "Friend"), new("private", "Private"), new("protected", "Protected"), new("protectedInternal", "Protected Friend"), new("privateProtected", "Private Protected"), new("local", "Local")],
                "symbolSpecifications.requiredModifiers" => _language == LanguageNames.CSharp
                    ? [new("abstract", "abstract"), new("async", "async"), new("const", "const"), new("readonly", "readonly"), new("static", "static")]
                    : [new("abstract", "MustInherit"), new("async", "Async"), new("const", "Const"), new("readonly", "ReadOnly"), new("static", "Shared")],
                "namingStyles.capitalization" => [
                    new("pascalCase", ServicesVSResources.Pascal_Case_Name),
                    new("camelCase", ServicesVSResources.camel_Case_Name),
                    new("firstUpper", ServicesVSResources.First_word_upper),
                    new("allUpper", ServicesVSResources.ALL_UPPER),
                    new("allLower", ServicesVSResources.all_lower),
                ],
                _ => throw new InvalidDataException($"Unknown naming rule property '{enumSettingMoniker}'."),
            };
            return new ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>.Success(choices);
        }
        catch (InvalidDataException e)
        {
            return new ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>.Failure(GetError(e.Message), ExternalSettingsErrorScope.EntireRegion, isTransient: true);
        }
    }

    public OneOrMany<SettingMessage> ValidateSetting(string moniker, object value)
        => ValidateSettingCore(moniker, value);

    private OneOrMany<SettingMessage> ValidateSettingCore(string moniker, object value)
    {
        CheckMoniker(moniker);
        try
        {
            lock (_gate)
            {
                // The shell can validate synchronously during notification, before an asynchronous cache read.
                var graph = _pending ?? (_snapshot ??= _coordinator.ReadForValidation());
                _ = UpdateGraph(graph, moniker, value).ToPreferences();
            }

            return default;
        }
        catch (InvalidDataException e)
        {
            return new(new SettingMessage(GetError(e.Message), MessageSeverity.Error));
        }
    }

    public OneOrMany<SettingMessage> ValidateArrayItemProperty(string arraySettingMoniker, int arrayItemIndex, string propertyMoniker, IReadOnlyList<IReadOnlyDictionary<string, object>> arraySettingContent)
    {
        CheckMoniker(arraySettingMoniker);
        if (arraySettingMoniker == "rules")
            return ValidateSettingCore(arraySettingMoniker, arraySettingContent);

        try
        {
            lock (_gate)
            {
                var graph = _pending ?? (_snapshot ??= _coordinator.ReadForValidation());
                var previous = arraySettingMoniker == "namingStyles" ? graph.NamingStyles : graph.SymbolSpecifications;
                if (arrayItemIndex < 0 || arrayItemIndex >= arraySettingContent.Count
                    || arraySettingContent.Count < previous.Length || arraySettingContent.Count > previous.Length + 1)
                {
                    throw new InvalidDataException("Naming definition rows changed while the editor was open. Reload the settings before editing.");
                }

                // Other fields in an add/edit dialog can still be incomplete; save validates the entire graph.
                NamingSettingsGraph.ValidateDefinitionProperty(arraySettingContent[arrayItemIndex], propertyMoniker, isStyle: arraySettingMoniker == "namingStyles");
            }
            return default;
        }
        catch (InvalidDataException e)
        {
            return new(new SettingMessage(e.Message, MessageSeverity.Error));
        }
    }

    public Task<string> GetMessageTextAsync(string messageId, CancellationToken cancellationToken)
        => throw new InvalidOperationException($"Unknown naming message '{messageId}'.");

    public Task OpenBackingStoreAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException("The Naming settings region does not register a backing-store link.");

    private void OnChanged(object? sender, EventArgs args)
    {
        lock (_gate)
        {
            // An incoming import must not silently discard an open editor's changes. Apply checks the snapshot.
            if (_pending is not null)
            {
                return;
            }
            _snapshot = null;
        }

        RaiseRefresh();
    }

    public void NotifyErrorConditionResolved() => ErrorConditionResolved?.Invoke(this, EventArgs.Empty);

    private void RaiseRefresh()
    {
        SettingValuesChanged?.Invoke(this, ExternalSettingsChangedEventArgs.SomeOrAll);
        EnumSettingChoicesChanged?.Invoke(this, new("rules.symbolSpecificationId"));
        EnumSettingChoicesChanged?.Invoke(this, new("rules.namingStyleId"));
        EnumSettingChoicesChanged?.Invoke(this, new("symbolSpecifications.symbolKinds"));
        RevalidationRequired?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<EnumChoice> GetSymbolKindChoices(NamingSettingsGraph graph)
    {
        var choices = _language == LanguageNames.CSharp
            ? new List<EnumChoice> {
                new("namespace", ServicesVSResources.NamingSpecification_CSharp_Namespace), new("class", ServicesVSResources.NamingSpecification_CSharp_Class),
                new("struct", ServicesVSResources.NamingSpecification_CSharp_Struct), new("interface", ServicesVSResources.NamingSpecification_CSharp_Interface),
                new("enum", ServicesVSResources.NamingSpecification_CSharp_Enum), new("property", ServicesVSResources.NamingSpecification_CSharp_Property),
                new("method", ServicesVSResources.NamingSpecification_CSharp_Method), new("localFunction", ServicesVSResources.NamingSpecification_CSharp_LocalFunction),
                new("field", ServicesVSResources.NamingSpecification_CSharp_Field), new("event", ServicesVSResources.NamingSpecification_CSharp_Event),
                new("delegate", ServicesVSResources.NamingSpecification_CSharp_Delegate), new("parameter", ServicesVSResources.NamingSpecification_CSharp_Parameter),
                new("typeParameter", ServicesVSResources.NamingSpecification_CSharp_TypeParameter), new("local", ServicesVSResources.NamingSpecification_CSharp_Local),
            }
            : new List<EnumChoice> {
                new("namespace", ServicesVSResources.NamingSpecification_VisualBasic_Namespace), new("class", ServicesVSResources.NamingSpecification_VisualBasic_Class),
                new("struct", ServicesVSResources.NamingSpecification_VisualBasic_Structure), new("interface", ServicesVSResources.NamingSpecification_VisualBasic_Interface),
                new("enum", ServicesVSResources.NamingSpecification_VisualBasic_Enum), new("module", ServicesVSResources.NamingSpecification_VisualBasic_Module),
                new("property", ServicesVSResources.NamingSpecification_VisualBasic_Property), new("method", ServicesVSResources.NamingSpecification_VisualBasic_Method),
                new("field", ServicesVSResources.NamingSpecification_VisualBasic_Field), new("event", ServicesVSResources.NamingSpecification_VisualBasic_Event),
                new("delegate", ServicesVSResources.NamingSpecification_VisualBasic_Delegate), new("parameter", ServicesVSResources.NamingSpecification_VisualBasic_Parameter),
                new("typeParameter", ServicesVSResources.NamingSpecification_VisualBasic_TypeParameter), new("local", ServicesVSResources.NamingSpecification_VisualBasic_Local),
            };
        // Preserve imported kinds outside the legacy UI's offered subset.
        foreach (var token in graph.SymbolSpecifications.SelectMany(item => NamingSettingsGraph.ReadStrings(item, "symbolKinds")).Distinct(StringComparer.Ordinal))
        {
            if (!choices.Any(choice => choice.Moniker == token))
                choices.Add(new(token, token));
        }
        return choices;
    }

    private static Dictionary<string, object> CloneItem(IReadOnlyDictionary<string, object> item)
        => item.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static Dictionary<string, object> DisplayDefinition(IReadOnlyDictionary<string, object> item, string name, bool isStyle)
    {
        var result = CloneItem(item);
        result["name"] = name;
        result.Remove("legacyId");
        result.Remove("defaultName");
        if (!isStyle)
        {
            foreach (var property in new[] { "symbolKinds", "accessibilities", "requiredModifiers" })
                result[property] = NamingSettingsGraph.ReadStrings(item, property).ToArray();
        }
        return result;
    }

    private static NamingSettingsGraph UpdateGraph(NamingSettingsGraph graph, string moniker, object value)
        => (moniker switch
        {
            "rules" => new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles, ReadItems(value)),
            "symbolSpecifications" => new NamingSettingsGraph(ReadDefinitions(graph, value, isStyle: false), graph.NamingStyles, graph.Rules),
            "namingStyles" => new NamingSettingsGraph(graph.SymbolSpecifications, ReadDefinitions(graph, value, isStyle: true), graph.Rules),
            _ => throw new InvalidDataException($"Unknown naming setting '{moniker}'."),
        }).NormalizeDefaultNames();

    private static ImmutableArray<IReadOnlyDictionary<string, object>> ReadDefinitions(NamingSettingsGraph graph, object value, bool isStyle)
    {
        var previous = isStyle ? graph.NamingStyles : graph.SymbolSpecifications;
        var byId = previous.ToDictionary(item => NamingSettingsGraph.ReadString(item, "id"), StringComparer.Ordinal);
        var usedIds = new HashSet<string>(byId.Keys.Concat(NamingSettingsIdentity.GetBuiltInIds(isStyle ? "namingStyle" : "symbolSpecification")), StringComparer.Ordinal);
        var result = ImmutableArray.CreateBuilder<IReadOnlyDictionary<string, object>>();
        foreach (var item in ReadItems(value))
        {
            var name = NamingSettingsGraph.ReadString(item, "name");
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException(ServicesVSResources.Enter_a_title_for_this_Naming_Style);
            IReadOnlyDictionary<string, object>? original = null;
            if (item.TryGetValue("id", out var id))
            {
                if (id is not string identifier || !byId.TryGetValue(identifier, out original))
                    throw new InvalidDataException("Existing naming definition identities cannot be changed.");
            }
            var updated = original is null ? new Dictionary<string, object>(StringComparer.Ordinal) : CloneItem(original);
            if (original is null)
                updated["id"] = NamingSettingsIdentity.CreateId(Guid.Empty, name, usedIds);
            else if (item.TryGetValue("legacyId", out var legacyId) && (!original.TryGetValue("legacyId", out var existingLegacyId) || !Equals(legacyId, existingLegacyId)))
                throw new InvalidDataException("Existing naming compatibility identities cannot be changed.");

            updated["name"] = name;
            updated.Remove("legacyId");
            updated.Remove("defaultName");
            foreach (var property in isStyle ? new[] { "prefix", "suffix", "wordSeparator", "capitalization" } : new[] { "symbolKinds", "accessibilities", "requiredModifiers" })
                updated[property] = isStyle ? NamingSettingsGraph.ReadString(item, property) : NamingSettingsGraph.ReadStrings(item, property).ToArray();
            result.Add(updated);
        }
        return result.ToImmutable();
    }

    private static ImmutableArray<IReadOnlyDictionary<string, object>> ReadItems(object value)
        => value switch
        {
            IReadOnlyList<IReadOnlyDictionary<string, object>> rules => rules.Select(item =>
                (IReadOnlyDictionary<string, object>)item.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)).ToImmutableArray(),
            IReadOnlyList<IDictionary<string, object>> rules => rules.Select(item =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>(item, StringComparer.Ordinal)).ToImmutableArray(),
            _ => throw new InvalidDataException("Naming settings must be an array of objects."),
        };

    private static void CheckMoniker(string moniker)
    {
        if (moniker is not ("rules" or "symbolSpecifications" or "namingStyles"))
            throw new InvalidDataException($"Unknown naming setting '{moniker}'.");
    }

    private static string GetError(string detail) => string.Format(ServicesVSResources.Unable_to_update_naming_settings_0, detail);

    private static ExternalSettingOperationResult Failure(string detail)
        => new ExternalSettingOperationResult.Failure(GetError(detail), ExternalSettingsErrorScope.EntireRegion, isTransient: true);

    public void Dispose() => _coordinator.Changed -= OnChanged;
}
