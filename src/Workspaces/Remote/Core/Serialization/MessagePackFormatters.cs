// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
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
        internal static readonly ImmutableArray<IMessagePackFormatter> Formatters = ImmutableArray.Create<IMessagePackFormatter>(
            ProjectIdFormatter.Instance,
            EncodingFormatter.Instance,
            // ForceTypelessFormatter<T> needs to be listed here for each Roslyn abstract type T that is being serialized OOP.
            // TODO: add a resolver that provides these https://github.com/dotnet/roslyn/issues/60724
            new ForceTypelessFormatter<SimplifierOptions>(),
            new ForceTypelessFormatter<SyntaxFormattingOptions>(),
            new ForceTypelessFormatter<CodeGenerationOptions>(),
            new ForceTypelessFormatter<IdeCodeStyleOptions>());

        private static readonly ImmutableArray<IFormatterResolver> s_resolvers = ImmutableArray.Create<IFormatterResolver>(
            StandardResolverAllowPrivate.Instance);

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
