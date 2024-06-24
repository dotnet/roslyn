// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
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
        internal static readonly ImmutableArray<IMessagePackFormatter> Formatters =
        [
            ProjectIdFormatter.Instance,
            EncodingFormatter.Instance,
            new ForceTypelessFormatter<SimplifierOptions>(),
            new ForceTypelessFormatter<SyntaxFormattingOptions>(),
            new ForceTypelessFormatter<CodeGenerationOptions>(),
            new ForceTypelessFormatter<IdeCodeStyleOptions>(),
        ];

        private static readonly ImmutableArray<IFormatterResolver> s_resolvers = [StandardResolverAllowPrivate.Instance];

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
            /// Cache of previously (de)serialized ProjectIDs. This cache allows a particular ProjectId
            /// to only serialize or deserialize it's DebugName once. Additionally, this cache allows
            /// the Deserialization code to only construct the ProjectID a single time.
            /// </summary>
            private readonly ConcurrentDictionary<Guid, ProjectId> _projectIds = new ConcurrentDictionary<Guid, ProjectId>();

            public ProjectId? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                try
                {
                    if (reader.TryReadNil())
                    {
                        return null;
                    }

                    var arrayCount = reader.ReadArrayHeader();
                    Contract.ThrowIfFalse(arrayCount is 1 or 2);
                    var id = GuidFormatter.Instance.Deserialize(ref reader, options);
                    ProjectId? projectId;

                    if (arrayCount == 1)
                    {
                        // This ProjectId has previously been deserialized, attempt to find it
                        // in the cache.
                        if (!_projectIds.TryGetValue(id, out projectId))
                        {
                            // This *should* always succeed, but if not, it's ok to proceed with
                            // a new instance with everything correct but the debugName. Hopefully, 
                            // a later call will have the debugName and we'll update the cache.
                            projectId = ProjectId.CreateFromSerialized(id);
                            _projectIds.TryAdd(id, projectId);
                        }
                    }
                    else
                    {
                        // This is the first time this ProjectId has been deserialized, so read it's value.
                        // This id shouldn't be in our dictionary, but update if so.
                        var debugName = reader.ReadString();

                        projectId = ProjectId.CreateFromSerialized(id, debugName);
                        _projectIds[id] = projectId;
                    }

                    return projectId;
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
                        // Only serialize the ProjectId's DebugName if this is the first time we've serialized it.
                        var serializeDebugName = _projectIds.TryAdd(value.Id, value);

                        writer.WriteArrayHeader(serializeDebugName ? 2 : 1);
                        GuidFormatter.Instance.Serialize(ref writer, value.Id, options);

                        if (serializeDebugName)
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
