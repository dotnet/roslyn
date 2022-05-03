// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.CodeAnalysis.CodeGeneration;
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
            SolutionIdFormatter.Instance,
            ProjectIdFormatter.Instance,
            DocumentIdFormatter.Instance,
            // ForceTypelessFormatter<T> needs to be listed here for each Roslyn abstract type T that is being serialized OOP.
            // TODO: add a resolver that provides these https://github.com/dotnet/roslyn/issues/60724
            new ForceTypelessFormatter<SimplifierOptions>(),
            new ForceTypelessFormatter<SyntaxFormattingOptions>(),
            new ForceTypelessFormatter<CodeGenerationOptions>());

        private static readonly ImmutableArray<IFormatterResolver> s_resolvers = ImmutableArray.Create<IFormatterResolver>(
            StandardResolverAllowPrivate.Instance);

        internal static readonly IFormatterResolver DefaultResolver = CompositeResolver.Create(Formatters, s_resolvers);

        internal static IFormatterResolver CreateResolver(ImmutableArray<IMessagePackFormatter> additionalFormatters, ImmutableArray<IFormatterResolver> additionalResolvers)
            => (additionalFormatters.IsEmpty && additionalResolvers.IsEmpty) ? DefaultResolver : CompositeResolver.Create(Formatters.AddRange(additionalFormatters), s_resolvers.AddRange(additionalResolvers));

        internal sealed class SolutionIdFormatter : IMessagePackFormatter<SolutionId?>
        {
            public static readonly SolutionIdFormatter Instance = new SolutionIdFormatter();

            public SolutionId? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

                    return SolutionId.CreateFromSerialized(id, debugName);
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public void Serialize(ref MessagePackWriter writer, SolutionId? value, MessagePackSerializerOptions options)
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

        internal sealed class ProjectIdFormatter : IMessagePackFormatter<ProjectId?>
        {
            public static readonly ProjectIdFormatter Instance = new ProjectIdFormatter();

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

        internal sealed class DocumentIdFormatter : IMessagePackFormatter<DocumentId?>
        {
            public static readonly DocumentIdFormatter Instance = new DocumentIdFormatter();

            public DocumentId? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                try
                {
                    if (reader.TryReadNil())
                    {
                        return null;
                    }

                    Contract.ThrowIfFalse(reader.ReadArrayHeader() == 3);

                    var projectId = ProjectIdFormatter.Instance.Deserialize(ref reader, options);
                    Contract.ThrowIfNull(projectId);

                    var id = GuidFormatter.Instance.Deserialize(ref reader, options);
                    var debugName = reader.ReadString();

                    return DocumentId.CreateFromSerialized(projectId, id, debugName);
                }
                catch (Exception e) when (e is not MessagePackSerializationException)
                {
                    throw new MessagePackSerializationException(e.Message, e);
                }
            }

            public void Serialize(ref MessagePackWriter writer, DocumentId? value, MessagePackSerializerOptions options)
            {
                try
                {
                    if (value is null)
                    {
                        writer.WriteNil();
                    }
                    else
                    {
                        writer.WriteArrayHeader(3);
                        ProjectIdFormatter.Instance.Serialize(ref writer, value.ProjectId, options);
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
    }
}
