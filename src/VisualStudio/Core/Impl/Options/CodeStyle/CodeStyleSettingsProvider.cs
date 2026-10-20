// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

/// <summary>
/// Connects the settings UI to Roslyn's code-style and formatting options. Some controls edit different parts
/// of the same option, such as a code-style preference and its diagnostic severity. Changes are kept pending
/// until committed, then applied to the latest option values so parts the user did not edit are preserved.
/// </summary>
internal sealed class CodeStyleSettingsProvider : ICachingExternalSettingsProvider, ISuspendableExternalSettingsProvider, IDisposable
{
    private readonly IGlobalOptionService _globalOptions;
    private readonly Dictionary<string, CodeStyleSetting> _settings;
    private readonly Dictionary<string, object> _pending = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly string _language;
    private readonly Func<OptionStore>? _getPendingEditorConfigOptions;
    private readonly IThreadingContext? _threadingContext;
    private bool _disposed;
    private bool _suspended;

    internal string Language => _language;

    internal static CodeStyleSetting CreateStyleSetting(AbstractCodeStyleOptionViewModel item, string language)
    {
        var setting = new CodeStyleSetting(item.Option.Definition.ConfigName, item.Option, language, item.Description);
        var style = (ICodeStyleOption2)item.Option.DefaultValue!;
        var values = GetStylePreferenceValues(item.Option, language, item.Preferences);
        setting.Choices = values.Select((value, index) => new EnumChoice(setting.SerializePreference(style.WithValue(value)), item.Preferences[index].Name)).ToArray();
        return setting;
    }

    internal static object[] GetStylePreferenceValues(IOption2 option, string language, IEnumerable<CodeStylePreference> preferences)
    {
        var style = (ICodeStyleOption2)option.DefaultValue!;
        var type = style.Value!.GetType();
        // Legacy rows may expose a subset or a different order than the enum's numeric values.
        return type == typeof(bool)
            ? preferences.Select(preference => (object)preference.IsChecked).ToArray()
            : type == typeof(UnusedValuePreference)
                ? language == LanguageNames.VisualBasic
                    ? [UnusedValuePreference.UnusedLocalVariable]
                    : [UnusedValuePreference.UnusedLocalVariable, UnusedValuePreference.DiscardVariable]
            : type == typeof(ParenthesesPreference)
                ? [ParenthesesPreference.AlwaysForClarity, ParenthesesPreference.NeverIfUnnecessary]
            : type == typeof(UnusedParametersPreference)
                ? [UnusedParametersPreference.NonPublicMethods, UnusedParametersPreference.AllMethods]
            : type == typeof(NamespaceDeclarationPreference)
                ? [NamespaceDeclarationPreference.BlockScoped, NamespaceDeclarationPreference.FileScoped]
            : type == typeof(ExpressionBodyPreference)
                ? [ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine]
            : option.Definition.ConfigName == "csharp_prefer_braces"
                ? new[] { "Always", "None", "WhenMultiline" }.Select(name => Enum.Parse(type, name)).ToArray()
            : option.Definition.ConfigName == "csharp_using_directive_placement"
                ? new[] { "InsideNamespace", "OutsideNamespace" }.Select(name => Enum.Parse(type, name)).ToArray()
            : Enum.GetValues(type).Cast<object>().ToArray();
    }

    internal void ApplyPreviewValue(OptionStore store, string moniker, object? value)
    {
        if (value is null || FindSetting(moniker) is not { } setting)
            return;
        var severity = IsSeverityMoniker(moniker);
        if (!Equals(setting.Read(store, severity), value))
            store.SetOption(setting.Key.Option, setting.Key.Language, setting.Update(Read(store, setting.Key), value, severity));
    }

    internal static OptionStore CreateMetadataStore(IServiceProvider serviceProvider, IGlobalOptionService globalOptions, string language)
    {
        var store = new OptionStore(globalOptions);
        foreach (var option in serviceProvider.GetMefService<EditorConfigOptionsEnumerator>().GetOptions(language).SelectMany(group => group.options))
        {
            var key = new OptionKey2(option, option.IsPerLanguage ? language : null);
            if (Read(store, key) is ICodeStyleOption2 style && style.Notification.Severity is ReportDiagnostic.Suppress or ReportDiagnostic.Default)
                store.SetOption(option, key.Language, style.WithNotification(NotificationOption2.Silent));
        }
        return store;
    }

    internal static bool IsSeverityMoniker(string moniker)
        => moniker.EndsWith("_severity", StringComparison.Ordinal) || moniker.EndsWith("Severity", StringComparison.Ordinal) || moniker.EndsWith(".severity", StringComparison.Ordinal);

    internal CodeStyleSetting? FindSetting(string? moniker)
    {
        if (moniker is null)
            return null;

        if (moniker.EndsWith(".preference", StringComparison.Ordinal) || moniker.EndsWith(".severity", StringComparison.Ordinal))
            moniker = moniker[..moniker.LastIndexOf('.')];

        return _settings.Values.FirstOrDefault(setting =>
            MatchesMoniker(moniker, setting.Moniker) || MatchesMoniker(moniker, string.Join(".", setting.Moniker.Split('.').Select(CodeStyleSetting.ToUnifiedScalarMoniker))));
    }

    private static bool MatchesMoniker(string moniker, string settingMoniker)
        => moniker == settingMoniker || moniker == settingMoniker + "_severity" || moniker == settingMoniker + "Severity" ||
           moniker.EndsWith("." + settingMoniker, StringComparison.Ordinal) || moniker.EndsWith("." + settingMoniker + "_severity", StringComparison.Ordinal) || moniker.EndsWith("." + settingMoniker + "Severity", StringComparison.Ordinal);

    internal void NormalizePreviewNotifications(OptionStore store)
    {
        // Legacy preview rows do not support suppressed diagnostics; normalization affects only this private preview.
        foreach (var setting in _settings.Values)
        {
            if (Read(store, setting.Key) is ICodeStyleOption2 style && style.Notification.Severity is ReportDiagnostic.Suppress or ReportDiagnostic.Default)
                store.SetOption(setting.Key.Option, setting.Key.Language, style.WithNotification(NotificationOption2.Silent));
        }
    }

    public CodeStyleSettingsProvider(IGlobalOptionService globalOptions, string language, IEnumerable<CodeStyleSetting> settings, Func<OptionStore>? getPendingEditorConfigOptions = null, IThreadingContext? threadingContext = null)
    {
        _globalOptions = globalOptions;
        _language = language;
        _getPendingEditorConfigOptions = getPendingEditorConfigOptions;
        _threadingContext = threadingContext;
        _settings = settings.ToDictionary(setting => setting.Moniker, StringComparer.Ordinal);
        _globalOptions.AddOptionChangedHandler(this, OnOptionsChanged);
    }

    public event EventHandler<ExternalSettingsChangedEventArgs>? SettingValuesChanged;
    event EventHandler<EnumSettingChoicesChangedEventArgs>? IExternalSettingsProvider.EnumSettingChoicesChanged { add { } remove { } }
    event EventHandler<DynamicMessageTextChangedEventArgs>? IExternalSettingsProvider.DynamicMessageTextChanged { add { } remove { } }
    event EventHandler? IExternalSettingsProvider.ErrorConditionResolved { add { } remove { } }

    private static void OnOptionsChanged(object sender, object target, OptionChangedEventArgs args)
    {
        var provider = (CodeStyleSettingsProvider)target;
        lock (provider._gate)
        {
            if (provider._disposed || provider._suspended)
                return;
        }
        if (args.ChangedOptions.Any(change => provider._settings.Values.Any(setting => setting.Key.Equals(change.key))))
            provider.NotifyValuesChanged();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CodeStyleSettingsProvider));
    }

    private void NotifyValuesChanged()
    {
        lock (_gate)
        {
            if (_disposed || _suspended)
                return;
        }
        SettingValuesChanged?.Invoke(this, ExternalSettingsChangedEventArgs.SomeOrAll);
    }

    internal OptionStore CreatePreviewStore()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var store = new OptionStore(_globalOptions);
            foreach (var (moniker, value) in _pending)
            {
                var (setting, severity) = Resolve(moniker);
                store.SetOption(setting.Key.Option, setting.Key.Language, setting.Update(Read(store, setting.Key), value, severity));
            }
            return store;
        }
    }

    private (CodeStyleSetting setting, bool severity) Resolve(string moniker)
    {
        var severity = IsSeverityMoniker(moniker);
        var setting = FindSetting(moniker);
        if (setting is null)
            throw new ArgumentException(nameof(moniker));
        return (setting, severity);
    }

    internal static object Read(IOptionsReader reader, OptionKey2 key)
    {
        reader.TryGetOption<object>(key, out var value);
        return value!;
    }

    public Task<ExternalSettingOperationResult<T>> GetValueAsync<T>(string moniker, CancellationToken cancellationToken) where T : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ThrowIfDisposed();
            var (setting, severity) = Resolve(moniker);
            var value = setting.Read(CreatePreviewStore(), severity);
            return ExternalSettingOperationResult.ConvertSuccessResultTask<T>(value);
        }
    }

    public Task<ExternalSettingOperationResult> SetValueAsync<T>(string moniker, T value, CancellationToken cancellationToken) where T : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ThrowIfDisposed();
            var (setting, severity) = Resolve(moniker);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                setting.Update(Read(CreatePreviewStore(), setting.Key), value, severity);
            }
            catch (ArgumentException)
            {
                return Task.FromResult<ExternalSettingOperationResult>(new ExternalSettingOperationResult.Failure(ServicesVSResources.Not_a_valid_value, ExternalSettingsErrorScope.SingleSettingOnly, isTransient: true));
            }
            _pending[setting.Moniker + (severity ? "_severity" : string.Empty)] = value;
        }
        NotifyValuesChanged();
        return ExternalSettingOperationResult.SuccessResultTask();
    }

    public Task<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>> GetEnumChoicesAsync(string moniker, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CodeStyleSetting setting;
        bool severity;
        lock (_gate)
        {
            ThrowIfDisposed();
            (setting, severity) = Resolve(moniker);
        }
        if (severity)
        {
            var choices = new[] { NotificationOption2.Silent, NotificationOption2.Suggestion, NotificationOption2.Warning, NotificationOption2.Error }
                .Select(notification => new EnumChoice(CodeStyleSetting.SerializeSeverity(notification), notification.Severity.GetDisplayString())).ToArray();
            return ExternalSettingOperationResult.ConvertSuccessResultTask<IReadOnlyList<EnumChoice>>(choices);
        }
        return ExternalSettingOperationResult.ConvertSuccessResultTask<IReadOnlyList<EnumChoice>>(setting.GetChoices());
    }

    public Task<ExternalSettingOperationResult> CommitPendingChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            // Re-read legacy values rather than committing an old snapshot of the other compound half or bits.
            var store = CreatePreviewStore();
            cancellationToken.ThrowIfCancellationRequested();
            _globalOptions.SetGlobalOptions(store.GetChangedOptions().Select(change => new KeyValuePair<OptionKey2, object?>(change.key, change.newValue)).ToImmutableArray());
            _pending.Clear();
        }
        NotifyValuesChanged();
        return ExternalSettingOperationResult.SuccessResultTask();
    }

    public Task<ExternalSettingOperationResult> RefreshCacheAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            _pending.Clear();
        }
        NotifyValuesChanged();
        return ExternalSettingOperationResult.SuccessResultTask();
    }

    public Task SuspendAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_suspended)
            {
                _suspended = true;
                _globalOptions.RemoveOptionChangedHandler(this, OnOptionsChanged);
            }
        }
        return Task.CompletedTask;
    }
    public Task ResumeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (_suspended)
            {
                _globalOptions.AddOptionChangedHandler(this, OnOptionsChanged);
                _suspended = false;
            }
        }
        NotifyValuesChanged();
        return Task.CompletedTask;
    }
    public Task<string> GetMessageTextAsync(string messageId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
            ThrowIfDisposed();
        return Task.FromResult(string.Empty);
    }

    public async Task OpenBackingStoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
            ThrowIfDisposed();
        var threadingContext = _threadingContext ?? throw new InvalidOperationException();
        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        lock (_gate)
            ThrowIfDisposed();
        var serviceProvider = Shell.ServiceProvider.GlobalProvider;
        CodeStyleSettingsCommands.GenerateEditorConfig(serviceProvider, _getPendingEditorConfigOptions?.Invoke() ?? CreatePreviewStore(), _language);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            if (!_suspended)
                _globalOptions.RemoveOptionChangedHandler(this, OnOptionsChanged);
            _pending.Clear();
            SettingValuesChanged = null;
        }
    }
}
