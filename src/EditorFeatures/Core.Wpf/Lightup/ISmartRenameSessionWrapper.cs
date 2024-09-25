﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorFeatures.Lightup;

[Obsolete("Class has not been finalized and may change without warning.")]
internal readonly struct ISmartRenameSessionWrapper : INotifyPropertyChanged, IDisposable
{
    internal const string WrappedTypeName = "Microsoft.VisualStudio.Text.Editor.SmartRename.ISmartRenameSession";
    internal const string WrappedRenameContextTypeName = "Microsoft.VisualStudio.Text.Editor.SmartRename.RenameContext";
    private static readonly Type s_wrappedType;
    private static readonly Type? s_wrappedRenameContextType;

    private static readonly Func<object, TimeSpan> s_automaticFetchDelayAccessor;
    private static readonly Func<object, bool> s_isAvailableAccessor;
    private static readonly Func<object, bool> s_hasSuggestionsAccessor;
    private static readonly Func<object, bool> s_isInProgressAccessor;
    private static readonly Func<object, string> s_statusMessageAccessor;
    private static readonly Func<object, bool> s_statusMessageVisibilityAccessor;
    private static readonly Func<object, IReadOnlyList<string>> s_suggestedNamesAccessor;
    private static readonly Func<object?, object?>? s_renameContextImmutableListCreateBuilderAccessor;
    private static readonly Action<object, object>? s_renameContextImmutableListBuilderAddAccessor;
    private static readonly Func<object, object>? s_renameContextImmutableListBuilderToArrayAccessor;

    private static readonly Func<object, CancellationToken, Task<IReadOnlyList<string>>> s_getSuggestionsAsync;
    private static readonly Func<object, object, CancellationToken, Task<IReadOnlyList<string>>>? s_getSuggestionsAsync_WithContext;
    private static readonly Action<object> s_onCancel;
    private static readonly Action<object, string> s_onSuccess;

    private readonly object _instance;

    static ISmartRenameSessionWrapper()
    {
        s_wrappedType = typeof(AggregateFocusInterceptor).Assembly.GetType(WrappedTypeName, throwOnError: false, ignoreCase: false);
        s_wrappedRenameContextType = typeof(AggregateFocusInterceptor).Assembly.GetType(WrappedRenameContextTypeName, throwOnError: false, ignoreCase: false);

        s_automaticFetchDelayAccessor = LightupHelpers.CreatePropertyAccessor<object, TimeSpan>(s_wrappedType, nameof(AutomaticFetchDelay), TimeSpan.Zero);
        s_isAvailableAccessor = LightupHelpers.CreatePropertyAccessor<object, bool>(s_wrappedType, nameof(IsAvailable), false);
        s_hasSuggestionsAccessor = LightupHelpers.CreatePropertyAccessor<object, bool>(s_wrappedType, nameof(HasSuggestions), false);
        s_isInProgressAccessor = LightupHelpers.CreatePropertyAccessor<object, bool>(s_wrappedType, nameof(IsInProgress), false);
        s_statusMessageAccessor = LightupHelpers.CreatePropertyAccessor<object, string>(s_wrappedType, nameof(StatusMessage), "");
        s_statusMessageVisibilityAccessor = LightupHelpers.CreatePropertyAccessor<object, bool>(s_wrappedType, nameof(StatusMessageVisibility), false);
        s_suggestedNamesAccessor = LightupHelpers.CreatePropertyAccessor<object, IReadOnlyList<string>>(s_wrappedType, nameof(SuggestedNames), []);

        if (s_wrappedRenameContextType is not null)
        {
            s_renameContextImmutableListCreateBuilderAccessor = LightupHelpers.CreateGenericFunctionAccessor<object?, object?>(typeof(ImmutableArray),
                                                                                                                               nameof(ImmutableArray.CreateBuilder),
                                                                                                                               s_wrappedRenameContextType,
                                                                                                                               defaultValue: SpecializedTasks.Null<object>());

            s_renameContextImmutableListBuilderAddAccessor = LightupHelpers.CreateActionAccessor<object, object>(typeof(ImmutableArray<>.Builder).MakeGenericType(s_wrappedRenameContextType),
                                                                                                                 nameof(ImmutableArray<object>.Builder.Add),
                                                                                                                 s_wrappedRenameContextType);
            s_renameContextImmutableListBuilderToArrayAccessor = LightupHelpers.CreateFunctionAccessor<object, object>(typeof(ImmutableArray<>.Builder).MakeGenericType(s_wrappedRenameContextType),
                                                                                                                       nameof(ImmutableArray<object>.Builder.ToImmutable),
                                                                                                                       typeof(ImmutableArray<>).MakeGenericType(s_wrappedRenameContextType));

            var immutableArrayOfRenameContextType = typeof(ImmutableArray<>).MakeGenericType(s_wrappedRenameContextType);

            s_getSuggestionsAsync_WithContext = LightupHelpers.CreateFunctionAccessor<object, object, CancellationToken, Task<IReadOnlyList<string>>>(s_wrappedType,
                                                                                                                                                      nameof(GetSuggestionsAsync),
                                                                                                                                                      immutableArrayOfRenameContextType,
                                                                                                                                                      typeof(CancellationToken),
                                                                                                                                                      SpecializedTasks.EmptyReadOnlyList<string>());
        }

        s_getSuggestionsAsync = LightupHelpers.CreateFunctionAccessor<object, CancellationToken, Task<IReadOnlyList<string>>>(s_wrappedType, nameof(GetSuggestionsAsync), typeof(CancellationToken), SpecializedTasks.EmptyReadOnlyList<string>());
        s_onCancel = LightupHelpers.CreateActionAccessor<object>(s_wrappedType, nameof(OnCancel));
        s_onSuccess = LightupHelpers.CreateActionAccessor<object, string>(s_wrappedType, nameof(OnSuccess), typeof(string));
    }

    private ISmartRenameSessionWrapper(object instance)
    {
        _instance = instance;
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

    public Task<IReadOnlyList<string>> GetSuggestionsAsync(ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>> context, CancellationToken cancellationToken)
    {
        if (s_renameContextImmutableListCreateBuilderAccessor is not null &&
            s_renameContextImmutableListBuilderAddAccessor is not null &&
            s_renameContextImmutableListBuilderToArrayAccessor is not null &&
            s_getSuggestionsAsync_WithContext is not null)
        {
            // ImmutableArray<RenameContext>.CreateBuilder()
            var renameContextArrayBuilder = s_renameContextImmutableListCreateBuilderAccessor(arg: null);

            if (renameContextArrayBuilder is not null)
            {
                foreach (var (key, value) in context)
                {
                    foreach (var (filePath, content) in value)
                    {
                        // ImmutableArray<RenameContext>.Builder.Add(renameContext)
                        s_renameContextImmutableListBuilderAddAccessor(renameContextArrayBuilder, Activator.CreateInstance(s_wrappedRenameContextType, key, content, filePath));
                    }
                }

                // ImmutableArray<RenameContext>.Builder.ToImmutable()
                var renameContextArray = s_renameContextImmutableListBuilderToArrayAccessor(renameContextArrayBuilder);

                return s_getSuggestionsAsync_WithContext(_instance, renameContextArray, cancellationToken);
            }
        }

        // Fallback to no context version
        return s_getSuggestionsAsync(_instance, cancellationToken);
    }

    public void OnCancel()
        => s_onCancel(_instance);

    public void OnSuccess(string acceptedName)
        => s_onSuccess(_instance, acceptedName);

    public void Dispose()
        => ((IDisposable)_instance).Dispose();
}
