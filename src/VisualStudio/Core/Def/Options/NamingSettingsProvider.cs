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
using static Microsoft.VisualStudio.LanguageServices.Options.NamingSettingsConstants;

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
                SettingNames.Rules => graph.Rules.Select(CloneItem).ToArray(),
                SettingNames.SymbolSpecifications => graph.SymbolSpecifications.Select((item, index) => DisplayDefinition(item, preferences.SymbolSpecifications[index].Name ?? "", isStyle: false)).ToArray(),
                SettingNames.NamingStyles => graph.NamingStyles.Select((item, index) => DisplayDefinition(item, preferences.NamingStyles[index].Name ?? "", isStyle: true)).ToArray(),
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

    public async Task<ExternalSettingOperationResult> ResetAsync(CancellationToken cancellationToken)
    {
        NamingSettingsGraph? pending;
        lock (_gate)
            pending = _pending;

        try
        {
            var result = await _coordinator.ResetAsync(cancellationToken).ConfigureAwait(false);
            if (result.Outcome is not (SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued))
                return Failure($"{result.Outcome}. {result.Message}");

            lock (_gate)
            {
                _snapshot = NamingSettingsGraph.Default;
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
            var choices = enumSettingMoniker switch
            {
                Monikers.RuleSymbolSpecificationId => graph.SymbolSpecifications.Select((item, index) => new EnumChoice(
                    NamingSettingsGraph.ReadString(item, Properties.Id), preferences.SymbolSpecifications[index].Name ?? "")).ToArray(),
                Monikers.RuleNamingStyleId => graph.NamingStyles.Select((item, index) => new EnumChoice(
                    NamingSettingsGraph.ReadString(item, Properties.Id), preferences.NamingStyles[index].Name ?? "")).ToArray(),
                Monikers.RuleSeverity => [
                    new(Severities.Silent, WorkspacesResources.Refactoring_Only),
                    new(Severities.Suggestion, WorkspacesResources.Suggestion),
                    new(Severities.Warning, WorkspacesResources.Warning),
                    new(Severities.Error, WorkspacesResources.Error),
                ],
                Monikers.SpecificationSymbolKinds => GetSymbolKindChoices(graph),
                Monikers.SpecificationAccessibilities => _language == LanguageNames.CSharp
                    ? [new(Accessibilities.Public, "public"), new(Accessibilities.Internal, "internal"), new(Accessibilities.Private, "private"), new(Accessibilities.Protected, "protected"), new(Accessibilities.ProtectedInternal, "protected internal"), new(Accessibilities.PrivateProtected, "private protected"), new(Accessibilities.Local, "local")]
                    : [new(Accessibilities.Public, "Public"), new(Accessibilities.Internal, "Friend"), new(Accessibilities.Private, "Private"), new(Accessibilities.Protected, "Protected"), new(Accessibilities.ProtectedInternal, "Protected Friend"), new(Accessibilities.PrivateProtected, "Private Protected"), new(Accessibilities.Local, "Local")],
                Monikers.SpecificationRequiredModifiers => _language == LanguageNames.CSharp
                    ? [new(Modifiers.Abstract, "abstract"), new(Modifiers.Async, "async"), new(Modifiers.Const, "const"), new(Modifiers.ReadOnly, "readonly"), new(Modifiers.Static, "static")]
                    : [new(Modifiers.Abstract, "MustInherit"), new(Modifiers.Async, "Async"), new(Modifiers.Const, "Const"), new(Modifiers.ReadOnly, "ReadOnly"), new(Modifiers.Static, "Shared")],
                Monikers.StyleCapitalization => [
                    new(Capitalizations.PascalCase, ServicesVSResources.Pascal_Case_Name),
                    new(Capitalizations.CamelCase, ServicesVSResources.camel_Case_Name),
                    new(Capitalizations.FirstUpper, ServicesVSResources.First_word_upper),
                    new(Capitalizations.AllUpper, ServicesVSResources.ALL_UPPER),
                    new(Capitalizations.AllLower, ServicesVSResources.all_lower),
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
        if (arraySettingMoniker == SettingNames.Rules)
            return ValidateSettingCore(arraySettingMoniker, arraySettingContent);

        try
        {
            lock (_gate)
            {
                var graph = _pending ?? (_snapshot ??= _coordinator.ReadForValidation());
                var previous = arraySettingMoniker == SettingNames.NamingStyles ? graph.NamingStyles : graph.SymbolSpecifications;
                if (arrayItemIndex < 0 || arrayItemIndex >= arraySettingContent.Count
                    || arraySettingContent.Count < previous.Length || arraySettingContent.Count > previous.Length + 1)
                {
                    throw new InvalidDataException(ServicesVSResources.Naming_definition_rows_changed_while_the_editor_was_open);
                }

                // Other fields in an add/edit dialog can still be incomplete; save validates the entire graph.
                NamingSettingsGraph.ValidateDefinitionProperty(arraySettingContent[arrayItemIndex], propertyMoniker, isStyle: arraySettingMoniker == SettingNames.NamingStyles);
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
        EnumSettingChoicesChanged?.Invoke(this, new(Monikers.RuleSymbolSpecificationId));
        EnumSettingChoicesChanged?.Invoke(this, new(Monikers.RuleNamingStyleId));
        EnumSettingChoicesChanged?.Invoke(this, new(Monikers.SpecificationSymbolKinds));
        RevalidationRequired?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<EnumChoice> GetSymbolKindChoices(NamingSettingsGraph graph)
    {
        var choices = _language == LanguageNames.CSharp
            ? new List<EnumChoice> {
                new(SymbolKinds.Namespace, ServicesVSResources.NamingSpecification_CSharp_Namespace), new(SymbolKinds.Class, ServicesVSResources.NamingSpecification_CSharp_Class),
                new(SymbolKinds.Struct, ServicesVSResources.NamingSpecification_CSharp_Struct), new(SymbolKinds.Interface, ServicesVSResources.NamingSpecification_CSharp_Interface),
                new(SymbolKinds.Enum, ServicesVSResources.NamingSpecification_CSharp_Enum), new(SymbolKinds.Property, ServicesVSResources.NamingSpecification_CSharp_Property),
                new(SymbolKinds.Method, ServicesVSResources.NamingSpecification_CSharp_Method), new(SymbolKinds.LocalFunction, ServicesVSResources.NamingSpecification_CSharp_LocalFunction),
                new(SymbolKinds.Field, ServicesVSResources.NamingSpecification_CSharp_Field), new(SymbolKinds.Event, ServicesVSResources.NamingSpecification_CSharp_Event),
                new(SymbolKinds.Delegate, ServicesVSResources.NamingSpecification_CSharp_Delegate), new(SymbolKinds.Parameter, ServicesVSResources.NamingSpecification_CSharp_Parameter),
                new(SymbolKinds.TypeParameter, ServicesVSResources.NamingSpecification_CSharp_TypeParameter), new(SymbolKinds.Local, ServicesVSResources.NamingSpecification_CSharp_Local),
            }
            : new List<EnumChoice> {
                new(SymbolKinds.Namespace, ServicesVSResources.NamingSpecification_VisualBasic_Namespace), new(SymbolKinds.Class, ServicesVSResources.NamingSpecification_VisualBasic_Class),
                new(SymbolKinds.Struct, ServicesVSResources.NamingSpecification_VisualBasic_Structure), new(SymbolKinds.Interface, ServicesVSResources.NamingSpecification_VisualBasic_Interface),
                new(SymbolKinds.Enum, ServicesVSResources.NamingSpecification_VisualBasic_Enum), new(SymbolKinds.Module, ServicesVSResources.NamingSpecification_VisualBasic_Module),
                new(SymbolKinds.Property, ServicesVSResources.NamingSpecification_VisualBasic_Property), new(SymbolKinds.Method, ServicesVSResources.NamingSpecification_VisualBasic_Method),
                new(SymbolKinds.Field, ServicesVSResources.NamingSpecification_VisualBasic_Field), new(SymbolKinds.Event, ServicesVSResources.NamingSpecification_VisualBasic_Event),
                new(SymbolKinds.Delegate, ServicesVSResources.NamingSpecification_VisualBasic_Delegate), new(SymbolKinds.Parameter, ServicesVSResources.NamingSpecification_VisualBasic_Parameter),
                new(SymbolKinds.TypeParameter, ServicesVSResources.NamingSpecification_VisualBasic_TypeParameter), new(SymbolKinds.Local, ServicesVSResources.NamingSpecification_VisualBasic_Local),
            };
        // Preserve imported kinds outside the legacy UI's offered subset.
        foreach (var token in graph.SymbolSpecifications.SelectMany(item => NamingSettingsGraph.ReadStrings(item, Properties.SymbolKinds)).Distinct(StringComparer.Ordinal))
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
        result[Properties.Name] = name;
        result.Remove(Properties.LegacyId);
        result.Remove(Properties.DefaultName);
        if (!isStyle)
        {
            foreach (var property in new[] { Properties.SymbolKinds, Properties.Accessibilities, Properties.RequiredModifiers })
                result[property] = NamingSettingsGraph.ReadStrings(item, property).ToArray();
        }
        return result;
    }

    private static NamingSettingsGraph UpdateGraph(NamingSettingsGraph graph, string moniker, object value)
        => (moniker switch
        {
            SettingNames.Rules => new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles, ReadItems(value)),
            SettingNames.SymbolSpecifications => new NamingSettingsGraph(ReadDefinitions(graph, value, isStyle: false), graph.NamingStyles, graph.Rules),
            SettingNames.NamingStyles => new NamingSettingsGraph(graph.SymbolSpecifications, ReadDefinitions(graph, value, isStyle: true), graph.Rules),
            _ => throw new InvalidDataException($"Unknown naming setting '{moniker}'."),
        }).NormalizeDefaultNames();

    private static ImmutableArray<IReadOnlyDictionary<string, object>> ReadDefinitions(NamingSettingsGraph graph, object value, bool isStyle)
    {
        var previous = isStyle ? graph.NamingStyles : graph.SymbolSpecifications;
        var byId = previous.ToDictionary(item => NamingSettingsGraph.ReadString(item, Properties.Id), StringComparer.Ordinal);
        var usedIds = new HashSet<string>(byId.Keys.Concat(NamingSettingsIdentity.GetBuiltInIds(isStyle ? IdentityKinds.NamingStyle : IdentityKinds.SymbolSpecification)), StringComparer.Ordinal);
        var result = ImmutableArray.CreateBuilder<IReadOnlyDictionary<string, object>>();
        foreach (var item in ReadItems(value))
        {
            var name = NamingSettingsGraph.ReadString(item, Properties.Name);
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException(ServicesVSResources.Enter_a_name);
            IReadOnlyDictionary<string, object>? original = null;
            if (item.TryGetValue(Properties.Id, out var id))
            {
                if (id is not string identifier || !byId.TryGetValue(identifier, out original))
                    throw new InvalidDataException(ServicesVSResources.Existing_naming_definition_identities_cannot_be_changed);
            }
            var updated = original is null ? new Dictionary<string, object>(StringComparer.Ordinal) : CloneItem(original);
            if (original is null)
                updated[Properties.Id] = NamingSettingsIdentity.CreateId(Guid.Empty, name, usedIds);
            else if (item.TryGetValue(Properties.LegacyId, out var legacyId) && (!original.TryGetValue(Properties.LegacyId, out var existingLegacyId) || !Equals(legacyId, existingLegacyId)))
                throw new InvalidDataException(ServicesVSResources.Existing_naming_compatibility_identities_cannot_be_changed);

            updated[Properties.Name] = name;
            updated.Remove(Properties.LegacyId);
            updated.Remove(Properties.DefaultName);
            foreach (var property in isStyle ? new[] { Properties.Prefix, Properties.Suffix, Properties.WordSeparator, Properties.Capitalization } : new[] { Properties.SymbolKinds, Properties.Accessibilities, Properties.RequiredModifiers })
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
        if (moniker is not (SettingNames.Rules or SettingNames.SymbolSpecifications or SettingNames.NamingStyles))
            throw new InvalidDataException($"Unknown naming setting '{moniker}'.");
    }

    private static string GetError(string detail) => string.Format(ServicesVSResources.Unable_to_update_naming_settings_0, detail);

    private static ExternalSettingOperationResult Failure(string detail)
        => new ExternalSettingOperationResult.Failure(GetError(detail), ExternalSettingsErrorScope.EntireRegion, isTransient: true);

    public void Dispose() => _coordinator.Changed -= OnChanged;
}
