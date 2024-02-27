// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Defines MessagePack formatters for public types without a public constructor suitable for deserialization.
    /// Roslyn internal types should always be annotated with <see cref="DataContractAttribute"/> and have the right constructor.
    /// </summary>
    internal sealed class MessagePackFormatters
    {
        internal static readonly ImmutableArray<IMessagePackFormatter> Formatters =
        [
            ProjectIdFormatter.Instance,
            EncodingFormatter.Instance,
            new ForceTypelessFormatter<SimplifierOptions>(),
            new ForceTypelessFormatter<SyntaxFormattingOptions>(),
            new ForceTypelessFormatter<CodeGenerationOptions>(),
            new ForceTypelessFormatter<IdeCodeStyleOptions>(),
        ];

        public sealed class RoslynImmutableCollectionFormatterResolver : IFormatterResolver
        {
            public static RoslynImmutableCollectionFormatterResolver Instance = new();

            public IMessagePackFormatter<T>? GetFormatter<T>()
            {
                return FormatterCache<T>.Formatter;
            }

            private static class FormatterCache<T>
            {
                internal static readonly IMessagePackFormatter<T>? Formatter;

                static FormatterCache()
                {
                    Formatter = (IMessagePackFormatter<T>?)ImmutableCollectionGetFormatterHelper.GetFormatter(typeof(T));
                }
            }

            public class ImmutableSegmentedListFormatter<T> : IMessagePackFormatter<ImmutableSegmentedList<T>>
            {
                ImmutableSegmentedList<T> IMessagePackFormatter<ImmutableSegmentedList<T>>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    if (reader.TryReadNil())
                        return default;

                    var len = reader.ReadArrayHeader();
                    if (len == 0)
                        return ImmutableSegmentedList<T>.Empty;

                    var formatter = options.Resolver.GetFormatterWithVerify<T>();

                    // TODO: Should there be a CreateBuilder that takes in an initial capacity
                    // similar to what ImmutableArray allows?
                    var builder = ImmutableSegmentedList.CreateBuilder<T>();
                    options.Security.DepthStep(ref reader);
                    try
                    {
                        for (var i = 0; i < len; i++)
                            builder.Add(formatter.Deserialize(ref reader, options));
                    }
                    finally
                    {
                        reader.Depth--;
                    }

                    return builder.ToImmutable();
                }

                void IMessagePackFormatter<ImmutableSegmentedList<T>>.Serialize(ref MessagePackWriter writer, ImmutableSegmentedList<T> value, MessagePackSerializerOptions options)
                {
                    if (value.IsDefault)
                    {
                        writer.WriteNil();
                    }
                    else if (value.IsEmpty)
                    {
                        writer.WriteArrayHeader(0);
                    }
                    else
                    {
                        var formatter = options.Resolver.GetFormatterWithVerify<T>();

                        writer.WriteArrayHeader(value.Count);
                        foreach (var item in value)
                            formatter.Serialize(ref writer, item, options);
                    }
                }
            }

            internal static class ImmutableCollectionGetFormatterHelper
            {
                private static readonly Dictionary<Type, Type> s_formatterMap = new()
                {
                    { typeof(ImmutableSegmentedList<>), typeof(ImmutableSegmentedListFormatter<>) },
                };

                internal static object? GetFormatter(Type t)
                {
                    var ti = t.GetTypeInfo();

                    if (ti.IsGenericType)
                    {
                        var genericType = ti.GetGenericTypeDefinition();
                        //var genericTypeInfo = genericType.GetTypeInfo();
                        //var isNullable = genericTypeInfo.IsNullable();
                        //var nullableElementType = isNullable ? ti.GenericTypeArguments[0] : null;

                        if (s_formatterMap.TryGetValue(genericType, out var formatterType))
                        {
                            return CreateInstance(formatterType, ti.GenericTypeArguments);
                        }
                        //else if (isNullable && nullableElementType?.IsConstructedGenericType is true && nullableElementType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
                        //{
                        //    return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                        //}
                    }

                    return null;
                }

                private static object? CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
                {
                    return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
                }
            }
        }

        private static readonly ImmutableArray<IFormatterResolver> s_resolvers = [RoslynImmutableCollectionFormatterResolver.Instance, StandardResolverAllowPrivate.Instance];

        internal static readonly IFormatterResolver DefaultResolver = CompositeResolver.Create(Formatters, s_resolvers);

        internal static IFormatterResolver CreateResolver(ImmutableArray<IMessagePackFormatter> additionalFormatters, ImmutableArray<IFormatterResolver> additionalResolvers)
            => (additionalFormatters.IsEmpty && additionalResolvers.IsEmpty) ? DefaultResolver : CompositeResolver.Create(Formatters.AddRange(additionalFormatters), s_resolvers.AddRange(additionalResolvers));

        /// <summary>
        /// Specialized formatter used so we can cache and reuse <see cref="ProjectId"/> instances.  This is valuable as
        /// it's very common for a set of results to reuse the same ProjectId across long sequences of results
        /// containing <see cref="DocumentId"/>s.  This allows a single instance to be created and shared across that
        /// entire sequence, saving on allocations.
        /// </summary>
        internal sealed class ProjectIdFormatter : IMessagePackFormatter<ProjectId?>
        {
            public static readonly ProjectIdFormatter Instance = new();

            /// <summary>
            /// Keep a copy of the most recent project ID to avoid duplicate instances when many consecutive IDs
            /// reference the same project.
            /// </summary>
            /// <remarks>
            /// Synchronization is not required for this field, since it's only intended to be an opportunistic (lossy)
            /// cache.
            /// </remarks>
            private ProjectId? _previousProjectId;

            public ProjectId? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                try
                {
                    if (reader.TryReadNil())
                    {
                        return null;
                    }

                    Contract.ThrowIfFalse(reader.ReadArrayHeader() == 2);
                    var id = GuidFormatter.Instance.Deserialize(ref reader, options);
                    var debugName = reader.ReadString();

                    var previousId = _previousProjectId;
                    if (previousId is not null && previousId.Id == id && previousId.DebugName == debugName)
                        return previousId;

                    var currentId = ProjectId.CreateFromSerialized(id, debugName);
                    _previousProjectId = currentId;
                    return currentId;
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public void Serialize(ref MessagePackWriter writer, ProjectId? value, MessagePackSerializerOptions options)
            {
                try
                {
                    if (value is null)
                    {
                        writer.WriteNil();
                    }
                    else
                    {
                        writer.WriteArrayHeader(2);
                        GuidFormatter.Instance.Serialize(ref writer, value.Id, options);
                        writer.Write(value.DebugName);
                    }
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }
        }

        /// <summary>
        /// Supports (de)serialization of <see cref="Encoding"/> that do not customize <see cref="Encoding.EncoderFallback"/> or <see cref="Encoding.DecoderFallback"/>.
        /// The fallback will be discarded if the <see cref="Encoding"/> has any.
        /// </summary>
        /// <remarks>
        /// Only supports (de)serializing values that are statically typed to <see cref="Encoding"/>.
        /// This is important as we can't assume anything about arbitrary subtypes of <see cref="Encoding"/>
        /// and can only return general <see cref="Encoding"/> from the deserializer.
        /// </remarks>
        internal sealed class EncodingFormatter : IMessagePackFormatter<Encoding?>
        {
            public static readonly EncodingFormatter Instance = new();

            public Encoding? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                try
                {
                    if (reader.TryReadNil())
                    {
                        return null;
                    }

                    var kind = (TextEncodingKind)reader.ReadByte();
                    if (kind != TextEncodingKind.None)
                    {
                        return kind.GetEncoding();
                    }

                    var codePage = reader.ReadInt32();
                    if (codePage > 0)
                    {
                        return Encoding.GetEncoding(codePage);
                    }

                    var name = reader.ReadString();
                    if (name is null)
                    {
                        return null;
                    }

                    return Encoding.GetEncoding(name);
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public void Serialize(ref MessagePackWriter writer, Encoding? value, MessagePackSerializerOptions options)
            {
                try
                {
                    if (value is null)
                    {
                        writer.WriteNil();
                    }
                    else if (value.TryGetEncodingKind(out var kind))
                    {
                        Debug.Assert(kind != TextEncodingKind.None);
                        writer.WriteUInt8((byte)kind);
                    }
                    else
                    {
                        writer.WriteUInt8((byte)TextEncodingKind.None);
                        var codePage = value.CodePage;
                        writer.Write(codePage);
                        if (codePage <= 0)
                        {
                            writer.Write(value.WebName);
                        }
                    }
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }
        }
    }
}
