// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using MessagePack;
using MessagePack.Formatters;

// TODO: Copied from https://github.com/neuecc/MessagePack-CSharp/blob/master/src/MessagePack.ImmutableCollection/Formatters.cs.
// Replace with an implementation shipping with VS:
// https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1198374
// https://github.com/neuecc/MessagePack-CSharp/issues/606
//
// The following code includes fix for: https://github.com/neuecc/MessagePack-CSharp/issues/1033. Make sure the fix is included in the replacement.

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class ImmutableCollectionMessagePackResolver : IFormatterResolver
    {
        public static readonly ImmutableCollectionMessagePackResolver Instance = new ImmutableCollectionMessagePackResolver();

        private ImmutableCollectionMessagePackResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
            => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            internal static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>)GetFormatter(typeof(T));
            }
        }

        private static readonly Dictionary<Type, Type> s_formatterMap = new Dictionary<Type, Type>()
        {
            { typeof(ImmutableArray<>), typeof(ImmutableArrayFormatter<>) },
            { typeof(ImmutableList<>), typeof(ImmutableListFormatter<>) },
            { typeof(ImmutableDictionary<,>), typeof(ImmutableDictionaryFormatter<,>) },
            { typeof(ImmutableHashSet<>), typeof(ImmutableHashSetFormatter<>) },
            { typeof(ImmutableSortedDictionary<,>), typeof(ImmutableSortedDictionaryFormatter<,>) },
            { typeof(ImmutableSortedSet<>), typeof(ImmutableSortedSetFormatter<>) },
            { typeof(ImmutableQueue<>), typeof(ImmutableQueueFormatter<>) },
            { typeof(ImmutableStack<>), typeof(ImmutableStackFormatter<>) },
            { typeof(IImmutableList<>), typeof(InterfaceImmutableListFormatter<>) },
            { typeof(IImmutableDictionary<,>), typeof(InterfaceImmutableDictionaryFormatter<,>) },
            { typeof(IImmutableQueue<>), typeof(InterfaceImmutableQueueFormatter<>) },
            { typeof(IImmutableSet<>), typeof(InterfaceImmutableSetFormatter<>) },
            { typeof(IImmutableStack<>), typeof(InterfaceImmutableStackFormatter<>) },
        };

        internal static object GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();

            if (ti.IsGenericType)
            {
                var genericType = ti.GetGenericTypeDefinition();
                var genericTypeInfo = genericType.GetTypeInfo();
                var isNullable = genericTypeInfo.IsGenericType && genericTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
                var nullableElementType = isNullable ? ti.GenericTypeArguments[0] : null;

                if (s_formatterMap.TryGetValue(genericType, out var formatterType))
                {
                    return CreateInstance(formatterType, ti.GenericTypeArguments);
                }

                if (isNullable && nullableElementType.IsConstructedGenericType && nullableElementType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
                {
                    return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                }
            }

            return null;
        }

        private static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
            => Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);

        // ImmutableArray<T>.Enumerator is 'not' IEnumerator<T>, can't use abstraction layer.
        internal sealed class ImmutableArrayFormatter<T> : IMessagePackFormatter<ImmutableArray<T>>
        {
            public void Serialize(ref MessagePackWriter writer, ImmutableArray<T> value, MessagePackSerializerOptions options)
            {
                if (value.IsDefault)
                {
                    writer.WriteNil();
                }
                else
                {
                    var formatter = options.Resolver.GetFormatterWithVerify<T>();

                    writer.WriteArrayHeader(value.Length);

                    foreach (var item in value)
                    {
                        formatter.Serialize(ref writer, item, options);
                    }
                }
            }

            public ImmutableArray<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return default;
                }
                else
                {
                    var formatter = options.Resolver.GetFormatterWithVerify<T>();

                    var len = reader.ReadArrayHeader();

                    var builder = ImmutableArray.CreateBuilder<T>(len);
                    options.Security.DepthStep(ref reader);
                    try
                    {
                        for (var i = 0; i < len; i++)
                        {
                            builder.Add(formatter.Deserialize(ref reader, options));
                        }
                    }
                    finally
                    {
                        reader.Depth--;
                    }

                    return builder.ToImmutable();
                }
            }
        }

        internal sealed class ImmutableListFormatter<T> : CollectionFormatterBase<T, ImmutableList<T>.Builder, ImmutableList<T>.Enumerator, ImmutableList<T>>
        {
            protected override void Add(ImmutableList<T>.Builder collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection.Add(value);
            }

            protected override ImmutableList<T> Complete(ImmutableList<T>.Builder intermediateCollection)
            {
                return intermediateCollection.ToImmutable();
            }

            protected override ImmutableList<T>.Builder Create(int count, MessagePackSerializerOptions options)
            {
                return ImmutableList.CreateBuilder<T>();
            }

            protected override ImmutableList<T>.Enumerator GetSourceEnumerator(ImmutableList<T> source)
            {
                return source.GetEnumerator();
            }
        }

        internal sealed class ImmutableDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableDictionary<TKey, TValue>.Builder, ImmutableDictionary<TKey, TValue>.Enumerator, ImmutableDictionary<TKey, TValue>>
        {
            protected override void Add(ImmutableDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value, MessagePackSerializerOptions options)
            {
                collection.Add(key, value);
            }

            protected override ImmutableDictionary<TKey, TValue> Complete(ImmutableDictionary<TKey, TValue>.Builder intermediateCollection)
            {
                return intermediateCollection.ToImmutable();
            }

            protected override ImmutableDictionary<TKey, TValue>.Builder Create(int count, MessagePackSerializerOptions options)
            {
                return ImmutableDictionary.CreateBuilder<TKey, TValue>(options.Security.GetEqualityComparer<TKey>());
            }

            protected override ImmutableDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(ImmutableDictionary<TKey, TValue> source)
            {
                return source.GetEnumerator();
            }
        }

        internal sealed class ImmutableHashSetFormatter<T> : CollectionFormatterBase<T, ImmutableHashSet<T>.Builder, ImmutableHashSet<T>.Enumerator, ImmutableHashSet<T>>
        {
            protected override void Add(ImmutableHashSet<T>.Builder collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection.Add(value);
            }

            protected override ImmutableHashSet<T> Complete(ImmutableHashSet<T>.Builder intermediateCollection)
            {
                return intermediateCollection.ToImmutable();
            }

            protected override ImmutableHashSet<T>.Builder Create(int count, MessagePackSerializerOptions options)
            {
                return ImmutableHashSet.CreateBuilder<T>(options.Security.GetEqualityComparer<T>());
            }

            protected override ImmutableHashSet<T>.Enumerator GetSourceEnumerator(ImmutableHashSet<T> source)
            {
                return source.GetEnumerator();
            }
        }

        internal sealed class ImmutableSortedDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableSortedDictionary<TKey, TValue>.Builder, ImmutableSortedDictionary<TKey, TValue>.Enumerator, ImmutableSortedDictionary<TKey, TValue>>
        {
            protected override void Add(ImmutableSortedDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value, MessagePackSerializerOptions options)
            {
                collection.Add(key, value);
            }

            protected override ImmutableSortedDictionary<TKey, TValue> Complete(ImmutableSortedDictionary<TKey, TValue>.Builder intermediateCollection)
            {
                return intermediateCollection.ToImmutable();
            }

            protected override ImmutableSortedDictionary<TKey, TValue>.Builder Create(int count, MessagePackSerializerOptions options)
            {
                return ImmutableSortedDictionary.CreateBuilder<TKey, TValue>();
            }

            protected override ImmutableSortedDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(ImmutableSortedDictionary<TKey, TValue> source)
            {
                return source.GetEnumerator();
            }
        }

        internal sealed class ImmutableSortedSetFormatter<T> : CollectionFormatterBase<T, ImmutableSortedSet<T>.Builder, ImmutableSortedSet<T>.Enumerator, ImmutableSortedSet<T>>
        {
            protected override void Add(ImmutableSortedSet<T>.Builder collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection.Add(value);
            }

            protected override ImmutableSortedSet<T> Complete(ImmutableSortedSet<T>.Builder intermediateCollection)
            {
                return intermediateCollection.ToImmutable();
            }

            protected override ImmutableSortedSet<T>.Builder Create(int count, MessagePackSerializerOptions options)
            {
                return ImmutableSortedSet.CreateBuilder<T>();
            }

            protected override ImmutableSortedSet<T>.Enumerator GetSourceEnumerator(ImmutableSortedSet<T> source)
            {
                return source.GetEnumerator();
            }
        }

        // not best for performance(does not use ImmutableQueue<T>.Enumerator)
        internal sealed class ImmutableQueueFormatter<T> : CollectionFormatterBase<T, ImmutableQueueBuilder<T>, ImmutableQueue<T>>
        {
            protected override void Add(ImmutableQueueBuilder<T> collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection.Add(value);
            }

            protected override ImmutableQueue<T> Complete(ImmutableQueueBuilder<T> intermediateCollection)
            {
                return intermediateCollection.Q;
            }

            protected override ImmutableQueueBuilder<T> Create(int count, MessagePackSerializerOptions options)
            {
                return new ImmutableQueueBuilder<T>();
            }
        }

        // not best for performance(does not use ImmutableQueue<T>.Enumerator)
        internal sealed class ImmutableStackFormatter<T> : CollectionFormatterBase<T, T[], ImmutableStack<T>>
        {
            protected override void Add(T[] collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection[collection.Length - 1 - index] = value;
            }

            protected override ImmutableStack<T> Complete(T[] intermediateCollection)
            {
                return ImmutableStack.CreateRange(intermediateCollection);
            }

            protected override T[] Create(int count, MessagePackSerializerOptions options)
            {
                return new T[count];
            }
        }

        internal sealed class InterfaceImmutableListFormatter<T> : CollectionFormatterBase<T, ImmutableList<T>.Builder, IImmutableList<T>>
        {
            protected override void Add(ImmutableList<T>.Builder collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection.Add(value);
            }

            protected override IImmutableList<T> Complete(ImmutableList<T>.Builder intermediateCollection)
            {
                return intermediateCollection.ToImmutable();
            }

            protected override ImmutableList<T>.Builder Create(int count, MessagePackSerializerOptions options)
            {
                return ImmutableList.CreateBuilder<T>();
            }
        }

        internal sealed class InterfaceImmutableDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableDictionary<TKey, TValue>.Builder, IImmutableDictionary<TKey, TValue>>
        {
            protected override void Add(ImmutableDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value, MessagePackSerializerOptions options)
            {
                collection.Add(key, value);
            }

            protected override IImmutableDictionary<TKey, TValue> Complete(ImmutableDictionary<TKey, TValue>.Builder intermediateCollection)
            {
                return intermediateCollection.ToImmutable();
            }

            protected override ImmutableDictionary<TKey, TValue>.Builder Create(int count, MessagePackSerializerOptions options)
            {
                return ImmutableDictionary.CreateBuilder<TKey, TValue>(options.Security.GetEqualityComparer<TKey>());
            }
        }

        internal sealed class InterfaceImmutableSetFormatter<T> : CollectionFormatterBase<T, ImmutableHashSet<T>.Builder, IImmutableSet<T>>
        {
            protected override void Add(ImmutableHashSet<T>.Builder collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection.Add(value);
            }

            protected override IImmutableSet<T> Complete(ImmutableHashSet<T>.Builder intermediateCollection)
            {
                return intermediateCollection.ToImmutable();
            }

            protected override ImmutableHashSet<T>.Builder Create(int count, MessagePackSerializerOptions options)
            {
                return ImmutableHashSet.CreateBuilder<T>(options.Security.GetEqualityComparer<T>());
            }
        }

        internal sealed class InterfaceImmutableQueueFormatter<T> : CollectionFormatterBase<T, ImmutableQueueBuilder<T>, IImmutableQueue<T>>
        {
            protected override void Add(ImmutableQueueBuilder<T> collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection.Add(value);
            }

            protected override IImmutableQueue<T> Complete(ImmutableQueueBuilder<T> intermediateCollection)
            {
                return intermediateCollection.Q;
            }

            protected override ImmutableQueueBuilder<T> Create(int count, MessagePackSerializerOptions options)
            {
                return new ImmutableQueueBuilder<T>();
            }
        }

        internal sealed class InterfaceImmutableStackFormatter<T> : CollectionFormatterBase<T, T[], IImmutableStack<T>>
        {
            protected override void Add(T[] collection, int index, T value, MessagePackSerializerOptions options)
            {
                collection[collection.Length - 1 - index] = value;
            }

            protected override IImmutableStack<T> Complete(T[] intermediateCollection)
            {
                return ImmutableStack.CreateRange(intermediateCollection);
            }

            protected override T[] Create(int count, MessagePackSerializerOptions options)
            {
                return new T[count];
            }
        }

        // pseudo builders
        internal sealed class ImmutableQueueBuilder<T>
        {
            public ImmutableQueue<T> Q { get; set; } = ImmutableQueue<T>.Empty;

            public void Add(T value)
            {
                Q = Q.Enqueue(value);
            }
        }
    }
}
