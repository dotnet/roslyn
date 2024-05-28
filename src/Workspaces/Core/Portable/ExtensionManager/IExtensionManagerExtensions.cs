// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions;

internal static class IExtensionManagerExtensions
{
    public static void PerformAction(this IExtensionManager extensionManager, object extension, Action action)
    {
        try
        {
            if (!extensionManager.IsDisabled(extension))
            {
                action();
            }
        }
        catch (Exception e) when (extensionManager.HandleException(extension, e))
        {
        }
    }

    public static T PerformFunction<T>(
        this IExtensionManager extensionManager,
        object extension,
        Func<T> function,
        T defaultValue)
    {
        try
        {
            if (!extensionManager.IsDisabled(extension))
                return function();
        }
        catch (Exception e) when (extensionManager.HandleException(extension, e))
        {
        }

        return defaultValue;
    }

    public static async Task PerformActionAsync(
        this IExtensionManager extensionManager,
        object extension,
        Func<Task?> function)
    {
        try
        {
            if (!extensionManager.IsDisabled(extension))
            {
                var task = function() ?? Task.CompletedTask;
                await task.ConfigureAwait(false);
            }
        }
        catch (Exception e) when (extensionManager.HandleException(extension, e))
        {
        }
    }

    public static async Task<T> PerformFunctionAsync<T>(
        this IExtensionManager extensionManager,
        object extension,
        Func<CancellationToken, Task<T>?> function,
        T defaultValue,
        CancellationToken cancellationToken)
    {
        if (extensionManager.IsDisabled(extension))
            return defaultValue;

        try
        {
            var task = function(cancellationToken);
            if (task != null)
                return await task.ConfigureAwait(false);
        }
        catch (Exception e) when (extensionManager.HandleException(extension, e))
        {
        }

        return defaultValue;
    }

    [SuppressMessage("Style", "IDE0039:Use local function", Justification = "Avoid per-call delegate allocation")]
    public static Func<SyntaxNode, ImmutableArray<TExtension>> CreateNodeExtensionGetter<TExtension>(
        this IExtensionManager extensionManager, IEnumerable<TExtension> extensions, Func<TExtension, ImmutableArray<Type>> nodeTypeGetter)
    {
        var map = new Dictionary<Type, ImmutableArray<TExtension>>();

        foreach (var extension in extensions)
        {
            if (extension is null)
                continue;

            var types = extensionManager.PerformFunction(
                extension, () => nodeTypeGetter(extension), []);
            foreach (var type in types)
            {
                map[type] = map.TryGetValue(type, out var existing)
                    ? existing.Add(extension)
                    : [extension];
            }
        }

        return n => map.TryGetValue(n.GetType(), out var extensions) ? extensions : [];
    }

    [SuppressMessage("Style", "IDE0039:Use local function", Justification = "Avoid per-call delegate allocation")]
    public static Func<SyntaxToken, ImmutableArray<TExtension>> CreateTokenExtensionGetter<TExtension>(
        this IExtensionManager extensionManager, IEnumerable<TExtension> extensions, Func<TExtension, ImmutableArray<int>> tokenKindGetter)
    {
        var map = new Dictionary<int, ImmutableArray<TExtension>>();

        foreach (var extension in extensions)
        {
            if (extension is null)
                continue;

            var kinds = extensionManager.PerformFunction(
                extension, () => tokenKindGetter(extension), []);
            foreach (var kind in kinds)
            {
                map[kind] = map.TryGetValue(kind, out var existing)
                    ? existing.Add(extension)
                    : [extension];
            }
        }

        return t => map.TryGetValue(t.RawKind, out var extensions) ? extensions : [];
    }
}
