// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.SmartRename;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorFeatures.Lightup;

[Obsolete("Class has not been finalized and may change without warning.")]
internal readonly struct ISmartRenameSessionWrapper : INotifyPropertyChanged, IDisposable
{
    internal const string WrappedTypeName = "Microsoft.VisualStudio.Text.Editor.SmartRename.ISmartRenameSession";
    private static readonly Type s_wrappedType;

    private static readonly Func<object, TimeSpan> s_automaticFetchDelayAccessor;
    private static readonly Func<object, bool> s_isAvailableAccessor;
    private static readonly Func<object, bool> s_hasSuggestionsAccessor;
    private static readonly Func<object, bool> s_isInProgressAccessor;
    private static readonly Func<object, string> s_statusMessageAccessor;
    private static readonly Func<object, bool> s_statusMessageVisibilityAccessor;
    private static readonly Func<object, IReadOnlyList<string>> s_suggestedNamesAccessor;

    private static readonly Func<object, CancellationToken, Task<IReadOnlyList<string>>> s_getSuggestionsAsync;
    private static readonly Action<object> s_onCancel;
    private static readonly Action<object, string> s_onSuccess;

    private readonly object _instance;

    static ISmartRenameSessionWrapper()
    {
        s_wrappedType = typeof(AggregateFocusInterceptor).Assembly.GetType(WrappedTypeName, throwOnError: false, ignoreCase: false);

        s_automaticFetchDelayAccessor = LightupHelpers.CreatePropertyAccessor<object, TimeSpan>(s_wrappedType, nameof(AutomaticFetchDelay), TimeSpan.FromSeconds(2));
        s_isAvailableAccessor = LightupHelpers.CreatePropertyAccessor<object, bool>(s_wrappedType, nameof(IsAvailable), false);
        s_hasSuggestionsAccessor = LightupHelpers.CreatePropertyAccessor<object, bool>(s_wrappedType, nameof(HasSuggestions), false);
        s_isInProgressAccessor = LightupHelpers.CreatePropertyAccessor<object, bool>(s_wrappedType, nameof(IsInProgress), false);
        s_statusMessageAccessor = LightupHelpers.CreatePropertyAccessor<object, string>(s_wrappedType, nameof(StatusMessage), "");
        s_statusMessageVisibilityAccessor = LightupHelpers.CreatePropertyAccessor<object, bool>(s_wrappedType, nameof(StatusMessageVisibility), false);
        s_suggestedNamesAccessor = LightupHelpers.CreatePropertyAccessor<object, IReadOnlyList<string>>(s_wrappedType, nameof(SuggestedNames), []);

        s_getSuggestionsAsync = LightupHelpers.CreateFunctionAccessor<object, CancellationToken, Task<IReadOnlyList<string>>>(s_wrappedType, nameof(GetSuggestionsAsync), typeof(CancellationToken), SpecializedTasks.EmptyReadOnlyList<string>());
        s_onCancel = LightupHelpers.CreateActionAccessor<object>(s_wrappedType, nameof(OnCancel));
        s_onSuccess = LightupHelpers.CreateActionAccessor<object, string>(s_wrappedType, nameof(OnSuccess), typeof(string));
    }

    private ISmartRenameSessionWrapper(object instance)
    {
        this._instance = instance;
    }

    public TimeSpan AutomaticFetchDelay => s_automaticFetchDelayAccessor(_instance);
    public bool IsAvailable => s_isAvailableAccessor(_instance);
    public bool HasSuggestions => s_hasSuggestionsAccessor(_instance);
    public bool IsInProgress => s_isInProgressAccessor(_instance);
    public string StatusMessage => s_statusMessageAccessor(_instance);
    public bool StatusMessageVisibility => s_statusMessageVisibilityAccessor(_instance);
    public IReadOnlyList<string> SuggestedNames => s_suggestedNamesAccessor(_instance);

    public event PropertyChangedEventHandler PropertyChanged
    {
        add => ((INotifyPropertyChanged)_instance).PropertyChanged += value;
        remove => ((INotifyPropertyChanged)_instance).PropertyChanged -= value;
    }

    public static ISmartRenameSessionWrapper FromInstance(object? instance)
    {
        if (instance == null)
        {
            return default;
        }

        if (!IsInstance(instance))
        {
            throw new InvalidCastException($"Cannot cast '{instance.GetType().FullName}' to '{WrappedTypeName}'");
        }

        return new ISmartRenameSessionWrapper(instance);
    }

    public static bool IsInstance([NotNullWhen(true)] object? instance)
    {
        return instance != null && LightupHelpers.CanWrapObject(instance, s_wrappedType);
    }

    public Task<IReadOnlyList<string>> GetSuggestionsAsync(CancellationToken cancellationToken)
        => s_getSuggestionsAsync(_instance, cancellationToken);

    public void OnCancel()
        => s_onCancel(_instance);

    public void OnSuccess(string acceptedName)
        => s_onSuccess(_instance, acceptedName);

    public void Dispose()
        => ((IDisposable)_instance).Dispose();
}
