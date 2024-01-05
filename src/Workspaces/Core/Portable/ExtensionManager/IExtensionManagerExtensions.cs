// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Extensions
{
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
            Func<Task<T>?> function,
            T defaultValue)
        {
            if (extensionManager.IsDisabled(extension))
                return defaultValue;

            try
            {
                var task = function();
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
            var map = new ConcurrentDictionary<Type, ImmutableArray<TExtension>>();

            Func<Type, ImmutableArray<TExtension>> getExtensions = (Type t1) =>
            {
                var query = from e in extensions
                            let types = extensionManager.PerformFunction(e, () => nodeTypeGetter(e), ImmutableArray<Type>.Empty)
                            where !types.Any() || types.Any(static (t2, t1) => t1 == t2 || t1.GetTypeInfo().IsSubclassOf(t2), t1)
                            select e;

                return query.ToImmutableArray();
            };

            return n => map.GetOrAdd(n.GetType(), getExtensions);
        }

        [SuppressMessage("Style", "IDE0039:Use local function", Justification = "Avoid per-call delegate allocation")]
        public static Func<SyntaxToken, ImmutableArray<TExtension>> CreateTokenExtensionGetter<TExtension>(
            this IExtensionManager extensionManager, IEnumerable<TExtension> extensions, Func<TExtension, ImmutableArray<int>> tokenKindGetter)
        {
            var map = new ConcurrentDictionary<int, ImmutableArray<TExtension>>();
            Func<int, ImmutableArray<TExtension>> getExtensions = (int k) =>
            {
                var query = from e in extensions
                            let kinds = extensionManager.PerformFunction(e, () => tokenKindGetter(e), ImmutableArray<int>.Empty)
                            where !kinds.Any() || kinds.Contains(k)
                            select e;

                return query.ToImmutableArray();
            };

            return t => map.GetOrAdd(t.RawKind, getExtensions);
        }
    }
}
