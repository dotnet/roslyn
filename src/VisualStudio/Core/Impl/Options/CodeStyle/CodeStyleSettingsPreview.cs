// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.UnifiedSettings;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal sealed class CodeStyleSettingsPreviewFactory(
    IServiceProvider serviceProvider,
    Func<uint, CodeStyleSettingsProvider> getProvider,
    Func<OptionStore> getPendingOptions,
    Func<uint, OptionStore, AbstractOptionPreviewViewModel> createModel,
    uint maximumElementId,
    CancellationToken disposalToken) : IVsUIFactory
{
    private readonly List<WeakReference<CodeStyleSettingsPreview>> _previews = [];

    internal OptionStore CreatePendingStore()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var store = getPendingOptions();
        foreach (var reference in _previews)
        {
            if (reference.TryGetTarget(out var preview) && preview.IsLoaded)
                preview.ApplyViewModelValues(store);
        }
        return store;
    }

    public int CreateUIElement(ref Guid factory, uint elementId, out IVsUIElement element)
    {
        element = null!;
        if (disposalToken.IsCancellationRequested)
            return VSConstants.E_ABORT;
        if (elementId > maximumElementId)
            return VSConstants.E_INVALIDARG;
        ThreadHelper.ThrowIfNotOnUIThread();
        _previews.RemoveAll(reference => !reference.TryGetTarget(out _));
        element = new PreviewElement(serviceProvider, getProvider(elementId), store => createModel(elementId, store), preview => _previews.Add(new(preview)), disposalToken);
        return VSConstants.S_OK;
    }

    private sealed class PreviewElement(
        IServiceProvider serviceProvider,
        CodeStyleSettingsProvider provider,
        Func<OptionStore, AbstractOptionPreviewViewModel> createModel,
        Action<CodeStyleSettingsPreview> registerPreview,
        CancellationToken disposalToken) : WpfUIElement(typeof(CodeStyleSettingsPreview))
    {
        protected override void OnFrameworkElementCreated(FrameworkElement element)
        {
            var preview = (CodeStyleSettingsPreview)element;
            preview.Initialize(serviceProvider, provider, createModel, disposalToken);
            registerPreview(preview);
        }
    }
}

internal sealed class CodeStyleSettingsPreview : UserControl
{
    private CodeStyleSettingsProvider? _provider;
    private Func<OptionStore, AbstractOptionPreviewViewModel>? _createModel;
    private AbstractOptionPreviewViewModel? _model;
    private ISettingsViewModel? _settingsViewModel;
    private IThreadingContext? _threadingContext;
    private CancellationToken _disposalToken;
    private CancellationTokenRegistration _disposalRegistration;
    private readonly ScrollViewer _preview = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalContentAlignment = VerticalAlignment.Top,
    };

    public CodeStyleSettingsPreview()
    {
        Height = 150;
        VerticalAlignment = VerticalAlignment.Top;
        Content = _preview;
        _preview.PreviewMouseWheel += (_, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                return;

            // Hidden editor scrollbars do not stop the editor from consuming wheel input.
            e.Handled = true;
            if (!CanScroll(e.Delta) && TryScrollContainingPage(e.Delta))
                return;
            _preview.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = Mouse.MouseWheelEvent,
            });
        };
        DataContextChanged += (_, _) => ConnectDataContext();
        Loaded += (_, _) => Connect();
        Unloaded += (_, _) => Disconnect();
    }

    private bool CanScroll(int delta)
        => delta > 0 ? _preview.VerticalOffset > 0 : _preview.VerticalOffset < _preview.ScrollableHeight;

    private bool TryScrollContainingPage(int delta)
    {
        for (var parent = GetParent(this); parent is not null; parent = GetParent(parent))
        {
            if (parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - delta);
                return true;
            }
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject element)
        => element is FrameworkElement { Parent: not null } frameworkElement
            ? frameworkElement.Parent
            : VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element);

    internal void Initialize(IServiceProvider serviceProvider, CodeStyleSettingsProvider provider, Func<OptionStore, AbstractOptionPreviewViewModel> createModel, CancellationToken disposalToken)
    {
        _disposalToken = disposalToken;
        _threadingContext = serviceProvider.GetMefService<IThreadingContext>();
        _provider = provider;
        _createModel = createModel;
    }

    private void Connect()
    {
        if (_provider is null || _disposalToken.IsCancellationRequested)
            return;
        _provider.SettingValuesChanged -= OnSettingsChanged;
        _provider.SettingValuesChanged += OnSettingsChanged;
        _disposalRegistration.Dispose();
        _disposalRegistration = _disposalToken.Register(() => RunOnUIThread(Disconnect, CancellationToken.None));
        ConnectDataContext();
    }

    private void ConnectDataContext()
    {
        if (_settingsViewModel is not null)
        {
            _settingsViewModel.PropertyChanged -= OnActiveSettingChanged;
            foreach (var setting in _settingsViewModel.Settings.Values)
                setting.PropertyChanged -= OnActiveSettingChanged;
        }
        _settingsViewModel = DataContext as ISettingsViewModel;
        if (IsLoaded && _settingsViewModel is not null)
        {
            _settingsViewModel.PropertyChanged += OnActiveSettingChanged;
            foreach (var setting in _settingsViewModel.Settings.Values)
                setting.PropertyChanged += OnActiveSettingChanged;
        }
        if (IsLoaded)
            UpdatePreview();
    }

    private void OnActiveSettingChanged(object? sender, PropertyChangedEventArgs e)
        => QueuePreviewUpdate();

    private void OnSettingsChanged(object? sender, ExternalSettingsChangedEventArgs e)
        => QueuePreviewUpdate();

    private void QueuePreviewUpdate()
        => RunOnUIThread(() =>
        {
            if (IsLoaded && !_disposalToken.IsCancellationRequested)
            {
                UpdatePreview();
            }
        }, _disposalToken);

    private void RunOnUIThread(Action action, CancellationToken cancellationToken)
    {
        if (_threadingContext is not { } threadingContext)
            return;
        threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            action();
        }).Task.ReportNonFatalErrorUnlessCancelledAsync(cancellationToken);
    }

    private void UpdatePreview()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_provider is null || _createModel is null || _disposalToken.IsCancellationRequested)
            return;
        _preview.Content = null;
        _model?.Dispose();
        var store = _provider.CreatePreviewStore();
        ApplyViewModelValues(store);
        _provider.NormalizePreviewNotifications(store);
        _model = _createModel(store);
        var setting = _provider.FindSetting(_settingsViewModel?.ActiveSetting?.Setting.Moniker);
        var style = _model.CodeStyleItems.FirstOrDefault(item => setting is null || item.Option == setting.Key.Option);
        var checkBox = _model.Items.OfType<AbstractCheckBoxViewModel>().FirstOrDefault(item => setting is null || (item.Option == setting.Key.Option && (setting.Flag is null || item.Description == setting.Description)));
        var radio = _model.Items.OfType<AbstractRadioButtonViewModel>().FirstOrDefault();
        var text = style?.GetPreview() ?? checkBox?.GetPreview() ?? radio?.Preview;
        if (text is null)
            return;
        _model.UpdatePreview(text);
        if (_model.TextViewHost is { } host)
            ConfigureEditor(host.TextView);
        _preview.Content = _model.TextViewHost?.HostControl;
    }

    internal static void ConfigureEditor(IWpfTextView view)
    {
        view.Options.SetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId, false);
        view.Options.SetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId, false);
        view.VisualElement.VerticalAlignment = VerticalAlignment.Top;
        // Let the Auto ScrollViewer scroll the full preview rather than an editor-sized viewport.
        view.VisualElement.Height = view.LineHeight * view.TextSnapshot.LineCount;
    }

    internal void ApplyViewModelValues(OptionStore store)
    {
        if (_provider is null || _settingsViewModel is null)
            return;
        foreach (var setting in _settingsViewModel.Settings.Values)
            _provider.ApplyPreviewValue(store, setting.Moniker, setting.Value);
    }

    private void Disconnect()
    {
        _disposalRegistration.Dispose();
        if (_provider is not null)
            _provider.SettingValuesChanged -= OnSettingsChanged;
        if (_settingsViewModel is not null)
        {
            _settingsViewModel.PropertyChanged -= OnActiveSettingChanged;
            foreach (var setting in _settingsViewModel.Settings.Values)
                setting.PropertyChanged -= OnActiveSettingChanged;
        }
        _preview.Content = null;
        _model?.Dispose();
        _model = null;
    }
}
