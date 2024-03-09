// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RoslynImmutableCollectionFormatterResolver : IFormatterResolver
{
    public static RoslynImmutableCollectionFormatterResolver Instance = new();

    private static readonly Dictionary<Type, Type> s_formatterMap = new()
        {
            { typeof(ImmutableSegmentedList<>), typeof(ImmutableSegmentedListFormatter<>) },
        };

    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    private static class FormatterCache<T>
    {
        internal static readonly IMessagePackFormatter<T>? Formatter;

        static FormatterCache()
        {
            Formatter = (IMessagePackFormatter<T>?)GetFormatter(typeof(T));
        }

        private static object? GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();

            if (ti.IsGenericType)
            {
                var genericType = ti.GetGenericTypeDefinition();

                if (s_formatterMap.TryGetValue(genericType, out var formatterType))
                    return CreateInstance(formatterType, ti.GenericTypeArguments);
            }

            return null;
        }

        private static object? CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }
}
