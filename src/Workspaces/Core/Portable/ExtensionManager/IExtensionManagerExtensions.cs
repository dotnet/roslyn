// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e) when (extensionManager.CanHandleException(extension, e))
            {
                extensionManager.HandleException(extension, e);
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
                {
                    return function();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e) when (extensionManager.CanHandleException(extension, e))
            {
                extensionManager.HandleException(extension, e);
            }

            return defaultValue;
        }

        public static async Task PerformActionAsync(
            this IExtensionManager extensionManager,
            object extension,
            Func<Task> function)
        {
            try
            {
                if (!extensionManager.IsDisabled(extension))
                {
                    var task = function() ?? Task.CompletedTask;
                    await task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e) when (extensionManager.CanHandleException(extension, e))
            {
                extensionManager.HandleException(extension, e);
            }
        }

        public static async Task<T> PerformFunctionAsync<T>(
            this IExtensionManager extensionManager,
            object extension,
            Func<Task<T>> function,
            T defaultValue)
        {
            try
            {
                if (!extensionManager.IsDisabled(extension))
                {
                    var task = function() ?? SpecializedTasks.Default<T>();
                    return await task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e) when (extensionManager.CanHandleException(extension, e))
            {
                extensionManager.HandleException(extension, e);
            }

            return defaultValue;
        }

        public static Func<SyntaxNode, ImmutableArray<TExtension>> CreateNodeExtensionGetter<TExtension>(
            this IExtensionManager extensionManager, IEnumerable<TExtension> extensions, Func<TExtension, ImmutableArray<Type>> nodeTypeGetter)
        {
            var map = new ConcurrentDictionary<Type, ImmutableArray<TExtension>>();

            ImmutableArray<TExtension> GetExtensions(Type t1)
            {
                var query = from e in extensions
                            let types = extensionManager.PerformFunction(e, () => nodeTypeGetter(e), ImmutableArray<Type>.Empty)
                            where !types.Any() || types.Any(t2 => t1 == t2 || t1.GetTypeInfo().IsSubclassOf(t2))
                            select e;

                return query.ToImmutableArray();
            }

            return n => map.GetOrAdd(n.GetType(), GetExtensions);
        }

        public static Func<SyntaxToken, ImmutableArray<TExtension>> CreateTokenExtensionGetter<TExtension>(
            this IExtensionManager extensionManager, IEnumerable<TExtension> extensions, Func<TExtension, ImmutableArray<int>> tokenKindGetter)
        {
            var map = new ConcurrentDictionary<int, ImmutableArray<TExtension>>();
            ImmutableArray<TExtension> GetExtensions(int k)
            {
                var query = from e in extensions
                            let kinds = extensionManager.PerformFunction(e, () => tokenKindGetter(e), ImmutableArray<int>.Empty)
                            where !kinds.Any() || kinds.Contains(k)
                            select e;

                return query.ToImmutableArray();
            }

            return t => map.GetOrAdd(t.RawKind, GetExtensions);
        }
    }
}
